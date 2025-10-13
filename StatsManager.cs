using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class StatsManager
{
    public AdventurerStats CurrentStats { get; private set; }
    
    // Configuration
    private readonly StatsConfig _config;
    
    // System references for integration
    private TimeManager _timeManager;
    private WeatherManager _weatherManager;
    private QuestManager _questManager;
    private PoIManager _poiManager;
    private JournalManager _journalManager;
    
    // Timing for stat updates
    private float _lastUpdateTime;
    private float _hungerDecayTimer;
    private float _tirednessIncreaseTimer;
    private float _lastPersonalReflectionTime;
    private const float PERSONAL_REFLECTION_COOLDOWN = 1800f; // 30 minutes in seconds
    
    // Cooldown for stat-based journal entries
    private float _lastStatJournalTime;
    private const float STAT_JOURNAL_COOLDOWN = 300f; // 5 minutes between stat journal entries
    
    // Events
    public event Action<StatType, float> StatChanged;
    public event Action<int> LevelUp;
    
    public StatsManager()
    {
        CurrentStats = new AdventurerStats();
        _config = new StatsConfig();
        
        // Subscribe to stat events
        CurrentStats.StatChanged += OnStatChanged;
        CurrentStats.LevelUp += OnLevelUp;
    }
    
    public void Initialize(TimeManager timeManager, WeatherManager weatherManager, 
                          QuestManager questManager, PoIManager poiManager, JournalManager journalManager)
    {
        _timeManager = timeManager;
        _weatherManager = weatherManager;
        _questManager = questManager;
        _poiManager = poiManager;
        _journalManager = journalManager;
        
        // Subscribe to external system events
        if (_timeManager != null)
        {
            _timeManager.HourPassed += OnHourPassed;
            _timeManager.DayChanged += OnDayChanged;
        }
        
        if (_weatherManager != null)
        {
            _weatherManager.WeatherChanged += OnWeatherChanged;
        }
        
        if (_questManager != null)
        {
            _questManager.QuestCompleted += OnQuestCompleted;
        }
        
        if (_poiManager != null)
        {
            _poiManager.PoIInteracted += OnPoIInteracted;
            _poiManager.PoIDiscovered += OnPoIDiscovered;
        }
        
        if (_journalManager != null)
        {
            _journalManager.NewBiomeDiscovered += OnBiomeDiscovered;
        }
    }
    
    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _lastUpdateTime += deltaTime;
        
        // Update hunger decay (decreases over time)
        _hungerDecayTimer += deltaTime;
        if (_hungerDecayTimer >= _config.HungerDecayInterval)
        {
            float hungerDecrease = _config.HungerDecayRate * (_hungerDecayTimer / _config.HungerDecayInterval);
            float oldHunger = CurrentStats.Hunger;
            CurrentStats.SetStat(StatType.Hunger, CurrentStats.Hunger - hungerDecrease);
            // Hunger decay applied successfully
            _hungerDecayTimer = 0f;
        }
        
        // Update tiredness based on time of day and activity
        _tirednessIncreaseTimer += deltaTime;
        if (_tirednessIncreaseTimer >= _config.TirednessUpdateInterval)
        {
            UpdateTirednessBasedOnTimeOfDay();
            _tirednessIncreaseTimer = 0f;
        }
        
        // Recalculate derived stats
        UpdateComfort();
        UpdateMood();
    }
    
    public void OnMovement(float distance)
    {
        // Called when adventurer moves - increases tiredness
        float tirednessIncrease = distance * _config.MovementTirednessRate;
        CurrentStats.SetStat(StatType.Tiredness, CurrentStats.Tiredness - tirednessIncrease);
    }
    
    private void UpdateComfort()
    {
        // Comfort = (Hunger * 0.4 + Tiredness * 0.4 + WeatherModifier * 0.2)
        float weatherModifier = GetWeatherComfortModifier();
        float newComfort = (CurrentStats.Hunger * 0.4f) + (CurrentStats.Tiredness * 0.4f) + (weatherModifier * 0.2f);
        CurrentStats.SetStat(StatType.Comfort, newComfort);
    }
    
    private void UpdateMood()
    {
        // Mood = (Comfort * 0.6 + RecentActivities * 0.4)
        float activityBonus = CurrentStats.GetDecayedActivityBonus();
        float newMood = (CurrentStats.Comfort * 0.6f) + (activityBonus * 0.4f);
        CurrentStats.SetStat(StatType.Mood, newMood);
    }
    
    private void UpdateTirednessBasedOnTimeOfDay()
    {
        if (_timeManager == null) return;
        
        float tirednessChange = 0f;
        
        switch (_timeManager.CurrentTimeOfDay)
        {
            case TimeOfDay.Dawn:
                // Dawn: slight tiredness recovery as you wake up
                tirednessChange = _config.NightRestRate * 0.3f;
                break;
                
            case TimeOfDay.Day:
                // During day: gradually get more tired as the day progresses
                float dayProgressFactor = _timeManager.TimeProgress * 0.5f + 0.5f; // 0.5 to 1.0 multiplier
                tirednessChange = -_config.DayTirednessRate * dayProgressFactor;
                break;
                
            case TimeOfDay.Dusk:
                // Dusk: moderate tiredness increase as day ends
                tirednessChange = -_config.DayTirednessRate * 0.8f;
                break;
                
            case TimeOfDay.Night:
                // During night: recover tiredness (rest)
                // Better recovery early in the night
                float nightProgressFactor = (1.0f - _timeManager.TimeProgress) * 0.5f + 0.5f; // 0.5 to 1.0 multiplier  
                tirednessChange = _config.NightRestRate * nightProgressFactor;
                break;
        }
        
        CurrentStats.SetStat(StatType.Tiredness, CurrentStats.Tiredness + tirednessChange);
    }
    
    private float GetWeatherComfortModifier()
    {
        if (_weatherManager == null) return 50f; // Neutral if no weather system
        
        return _weatherManager.CurrentWeather switch
        {
            WeatherType.Clear => 70f,    // +20 from base 50
            WeatherType.Cloudy => 60f,   // +10 from base 50
            WeatherType.Rain => 45f,     // -5 from base 50
            WeatherType.Snow => 40f,     // -10 from base 50
            WeatherType.Fog => 50f,      // Neutral
            _ => 50f
        };
    }
    
    // Event handlers for external systems
    private void OnHourPassed(float gameHour)
    {
        // Only record personal reflections every 30 minutes, not every hour
        _lastPersonalReflectionTime += 3600f; // Add 1 hour (since this fires every game hour)
        
        if (_lastPersonalReflectionTime >= PERSONAL_REFLECTION_COOLDOWN)
        {
            // Only record if stats are in interesting ranges
            if (CurrentStats.Hunger < 40f || CurrentStats.Tiredness < 40f || CurrentStats.Mood < 40f || CurrentStats.Mood > 80f)
            {
                bool mixedConditions = (CurrentStats.Hunger < 30f && CurrentStats.Tiredness < 30f) ||
                                     (CurrentStats.Hunger < 40f && CurrentStats.Mood < 40f) ||
                                     (CurrentStats.Tiredness < 40f && CurrentStats.Mood < 40f);
                
                var (reflectionTitle, reflectionMessage) = JournalEntryData.Instance.GetPersonalReflectionEntry(mixedConditions);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, reflectionTitle, reflectionMessage);
                _lastPersonalReflectionTime = 0f;
            }
        }
    }
    
    private void OnDayChanged(int day)
    {
        // Generate daily summary based on current stats
        _journalManager?.OnDayEndStatsRequest(CurrentStats);
    }
    
    private void OnWeatherChanged(WeatherType newWeather)
    {
        // Weather affects comfort and mood immediately
        UpdateComfort();
        UpdateMood();
        
        // Add journal entries for significant weather mood changes
        if (CurrentStats.Mood < 30f && (newWeather == WeatherType.Rain || newWeather == WeatherType.Snow))
        {
            var (weatherTitle, weatherMessage) = JournalEntryData.Instance.GetWeatherEntry(false);
            _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, weatherTitle, weatherMessage);
        }
        else if (CurrentStats.Mood > 70f && newWeather == WeatherType.Clear)
        {
            var (weatherTitle, weatherMessage) = JournalEntryData.Instance.GetWeatherEntry(true);
            _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, weatherTitle, weatherMessage);
        }
    }
    
    private void OnQuestCompleted(Quest quest)
    {
        // Award experience for quest completion
        float xpGained = _config.QuestCompletionXP;
        CurrentStats.AddExperience(xpGained);
        
        // Add temporary mood boost
        CurrentStats.AddRecentActivityBonus(15f);
        
        // Record quest completion with experience gain
        var (title, message) = JournalEntryData.Instance.GetPersonalReflectionEntry(false);
        _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, title, 
            $"Completing that quest was quite rewarding! I gained {xpGained} experience and feel more accomplished.");
    }
    
    private void OnPoIInteracted(PointOfInterest poi, Adventurer adventurer)
    {
        // Handle PoI-specific stat effects (includes journal entry)
        HandlePoIStatEffects(poi);
        
        // Award small experience for interaction
        float xpGained = _config.PoIInteractionXP;
        CurrentStats.AddExperience(xpGained);
        
        // Add a separate experience note if the XP gain is significant enough
        if (xpGained > 0)
        {
            var (expTitle, expMessage) = JournalEntryData.Instance.GetExperienceMilestoneEntry();
            _journalManager?.OnStatsEvent(JournalEntryType.StatMilestone, expTitle, 
                $"Interacting with {poi.Name} taught me something new. I gained {xpGained} experience.");
        }
    }
    
    private void OnPoIDiscovered(PointOfInterest poi)
    {
        // Award experience for discovery
        float xpGained = _config.PoIDiscoveryXP;
        CurrentStats.AddExperience(xpGained);
        
        // Small mood boost for discovery
        CurrentStats.AddRecentActivityBonus(5f);
        
        // Record discovery in journal with positive mood and experience
        var (title, message) = JournalEntryData.Instance.GetMoodEntry(true);
        _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, title, 
            $"Discovered {poi.Name}! The thrill of discovery fills me with excitement. I gained {xpGained} experience from this find.");
    }
    
    private void HandlePoIStatEffects(PointOfInterest poi)
    {
        switch (poi.Type)
        {
            case PoIType.Inn:
                // Inn regenerates hunger quickly
                RegenerateHunger(_config.InnHungerRegenRate);
                var (innTitle, innMessage) = JournalEntryData.Instance.GetPoIEntry("inn");
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, innTitle, innMessage);
                break;
                
            case PoIType.Cottage:
                // Cottage regenerates tiredness
                RegenerateTiredness(_config.CottageRestRegenRate);
                var (cottageTitle, cottageMessage) = JournalEntryData.Instance.GetPoIEntry("cottage");
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, cottageTitle, cottageMessage);
                break;
                
            case PoIType.Chapel:
                // Chapel provides mood boost
                CurrentStats.AddRecentActivityBonus(5f);
                var (chapelTitle, chapelMessage) = JournalEntryData.Instance.GetPoIEntry("chapel");
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, chapelTitle, chapelMessage);
                break;
                
            case PoIType.Farmhouse:
                // Farmhouse provides moderate hunger regeneration
                RegenerateHunger(_config.InnHungerRegenRate * 0.7f);
                var (farmTitle, farmMessage) = JournalEntryData.Instance.GetPoIEntry("farmhouse");
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, farmTitle, farmMessage);
                break;
                
            case PoIType.BerryBush:
                // Berry bush provides significant hunger regeneration
                RegenerateHunger(_config.BerryBushHungerRegenRate);
                var (berryTitle, berryMessage) = JournalEntryData.Instance.GetPoIEntry("berrybush");
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, berryTitle, berryMessage);
                break;
                
            case PoIType.Chicken:
                // Chicken provides eggs for hunger regeneration
                RegenerateHunger(_config.ChickenEggHungerRegenRate);
                var (chickenTitle, chickenMessage) = JournalEntryData.Instance.GetPoIEntry("chicken");
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, chickenTitle, chickenMessage);
                break;    
        }
    }
    
    private void RegenerateHunger(float amount)
    {
        CurrentStats.SetStat(StatType.Hunger, CurrentStats.Hunger + amount);
    }
    
    public void RegenerateTiredness(float amount)
    {
        float oldTiredness = CurrentStats.Tiredness;
        CurrentStats.SetStat(StatType.Tiredness, CurrentStats.Tiredness + amount);
        System.Console.WriteLine($"[REGEN DEBUG] RegenerateTiredness called: {oldTiredness:F1} + {amount:F2} = {CurrentStats.Tiredness:F1}");
    }
    
    private void OnStatChanged(StatType statType, float oldValue, float newValue)
    {
        StatChanged?.Invoke(statType, newValue);
        
        // Check for significant stat changes that warrant journal entries
        CheckForStatJournalEntries(statType, oldValue, newValue);
    }
    
    private void OnLevelUp(int newLevel)
    {
        LevelUp?.Invoke(newLevel);
        
        // Record level up in journal with varied messages
        var (title, message) = JournalEntryData.Instance.GetLevelUpEntry(newLevel);
        _journalManager?.OnStatsEvent(JournalEntryType.LevelUp, title, message);
    }
    
    private void CheckForStatJournalEntries(StatType statType, float oldValue, float newValue)
    {
        // Check cooldown to prevent spam
        float currentTime = _lastUpdateTime;
        bool canAddStatEntry = (currentTime - _lastStatJournalTime) >= STAT_JOURNAL_COOLDOWN;
        
        // Record journal entries for significant stat changes
        switch (statType)
        {
            case StatType.Hunger when newValue < 30f && oldValue >= 30f && canAddStatEntry:
                var (hungerTitle, hungerMessage) = JournalEntryData.Instance.GetHungerEntry(true);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, hungerTitle, hungerMessage);
                _lastStatJournalTime = currentTime;
                break;
                
            case StatType.Hunger when newValue > 70f && oldValue <= 70f && canAddStatEntry:
                var (hungerSatTitle, hungerSatMessage) = JournalEntryData.Instance.GetHungerEntry(false);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, hungerSatTitle, hungerSatMessage);
                _lastStatJournalTime = currentTime;
                break;
                
            case StatType.Tiredness when newValue < 30f && oldValue >= 30f && canAddStatEntry:
                var (tiredTitle, tiredMessage) = JournalEntryData.Instance.GetTirednessEntry(true);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, tiredTitle, tiredMessage);
                _lastStatJournalTime = currentTime;
                break;
                
            case StatType.Tiredness when newValue > 70f && oldValue <= 70f && canAddStatEntry:
                var (restedTitle, restedMessage) = JournalEntryData.Instance.GetTirednessEntry(false);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, restedTitle, restedMessage);
                _lastStatJournalTime = currentTime;
                break;
                
            case StatType.Mood when newValue > 80f && oldValue <= 80f && canAddStatEntry:
                var (moodHighTitle, moodHighMessage) = JournalEntryData.Instance.GetMoodEntry(true);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, moodHighTitle, moodHighMessage);
                _lastStatJournalTime = currentTime;
                break;
                
            case StatType.Mood when newValue < 30f && oldValue >= 30f && canAddStatEntry:
                var (moodLowTitle, moodLowMessage) = JournalEntryData.Instance.GetMoodEntry(false);
                _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, moodLowTitle, moodLowMessage);
                _lastStatJournalTime = currentTime;
                break;
        }
        
        // Record experience milestones
        if (statType == StatType.Experience)
        {
            int totalExpMilestone = ((int)CurrentStats.TotalExperience / 25) * 25; // Every 25 XP instead of 50
            int oldTotalExpMilestone = ((int)(CurrentStats.TotalExperience - (newValue - oldValue)) / 25) * 25;
            
            if (totalExpMilestone > oldTotalExpMilestone && totalExpMilestone > 0)
            {
                var (expTitle, expMessage) = JournalEntryData.Instance.GetExperienceMilestoneEntry();
                _journalManager?.OnStatsEvent(JournalEntryType.StatMilestone, expTitle, expMessage);
            }
        }
    }
    

    
    // Public methods for external systems to trigger stat changes
    public void OnZoneEntered(Zone zone)
    {
        // Award experience for zone discovery
        float xpGained = _config.ZoneDiscoveryXP;
        CurrentStats.AddExperience(xpGained);
        
        // Mood boost for exploration
        CurrentStats.AddRecentActivityBonus(5f);
        
        // Record zone exploration with positive mood and experience
        var (title, message) = JournalEntryData.Instance.GetMoodEntry(true);
        _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, title, 
            $"Exploring {zone.Name} fills me with wonder! I gained {xpGained} experience from discovering this new area.");
    }
    
    public void OnBiomeDiscovered(BiomeType biome)
    {
        // Award significant experience for biome discovery
        float xpGained = _config.BiomeDiscoveryXP;
        CurrentStats.AddExperience(xpGained);
        
        // Significant mood boost
        CurrentStats.AddRecentActivityBonus(10f);
        
        // Record biome discovery with high excitement and experience
        var (title, message) = JournalEntryData.Instance.GetMoodEntry(true);
        _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, title, 
            $"Discovering this {biome} biome fills me with wonder and excitement! I gained {xpGained} experience from this major discovery!");
    }
    
    public void OnSleepStarted()
    {
        var (title, message) = JournalEntryData.Instance.GetTirednessEntry(true);
        _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, title, 
            "I'm too exhausted to continue. Time to set up camp and get some rest.");
    }
    
    public void OnSleepEnded()
    {
        var (title, message) = JournalEntryData.Instance.GetTirednessEntry(false);
        _journalManager?.OnStatsEvent(JournalEntryType.PersonalReflection, title, 
            "That was a refreshing sleep! I feel energized and ready to continue my adventure.");
    }
    
    public void Dispose()
    {
        // Unsubscribe from all events to prevent memory leaks
        if (CurrentStats != null)
        {
            CurrentStats.StatChanged -= OnStatChanged;
            CurrentStats.LevelUp -= OnLevelUp;
        }
        
        if (_timeManager != null)
        {
            _timeManager.HourPassed -= OnHourPassed;
            _timeManager.DayChanged -= OnDayChanged;
        }
        
        if (_weatherManager != null)
        {
            _weatherManager.WeatherChanged -= OnWeatherChanged;
        }
        
        if (_questManager != null)
        {
            _questManager.QuestCompleted -= OnQuestCompleted;
        }
        
        if (_poiManager != null)
        {
            _poiManager.PoIInteracted -= OnPoIInteracted;
            _poiManager.PoIDiscovered -= OnPoIDiscovered;
        }
        
        if (_journalManager != null)
        {
            _journalManager.NewBiomeDiscovered -= OnBiomeDiscovered;
        }
    }
}

// Configuration class for stat rates and values
public class StatsConfig
{
    // Experience rewards
    public float QuestCompletionXP { get; set; } = 20f;
    public float PoIDiscoveryXP { get; set; } = 10f;
    public float PoIInteractionXP { get; set; } = 2f;
    public float ZoneDiscoveryXP { get; set; } = 20f;
    public float BiomeDiscoveryXP { get; set; } = 50f;
    
    // Stat decay rates
    public float HungerDecayRate { get; set; } = 5f; // Points per interval
    public float HungerDecayInterval { get; set; } = 5f; // 5 seconds for testing
    
    // Tiredness rates - synced with day/night cycle
    public float TirednessUpdateInterval { get; set; } = 30f; // Update every 30 seconds
    public float DayTirednessRate { get; set; } = 2f; // Points lost per update during day
    public float NightRestRate { get; set; } = 4f; // Points recovered per update during night
    public float MovementTirednessRate { get; set; } = 0.01f; // Points per pixel moved (realistic)
    
    // Regeneration rates
    public float InnHungerRegenRate { get; set; } = 80f;
    public float CottageRestRegenRate { get; set; } = 30f;
    public float BerryBushHungerRegenRate { get; set; } = 60f;
    public float ChickenEggHungerRegenRate { get; set; } = 50f;
}