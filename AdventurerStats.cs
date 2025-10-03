using System;

namespace HiddenHorizons;

public class AdventurerStats
{
    // Experience and leveling
    public int Level { get; set; } = 1;
    public float Experience { get; set; } = 0f;
    public float ExperienceToNext { get; set; } = 100f; // XP needed for next level
    public float TotalExperience { get; set; } = 0f; // Total XP earned (for journal milestones)
    
    // Core stats (0-100 range)
    public float Hunger { get; set; } = 100f;         // 100 = well fed, 0 = starving
    public float Tiredness { get; set; } = 100f;     // 100 = well rested, 0 = exhausted
    public float Comfort { get; set; } = 100f;       // Calculated from hunger/tiredness/weather
    public float Mood { get; set; } = 100f;          // Calculated from comfort/weather/activities
    
    // Temporary modifiers for mood calculation
    public float RecentActivityBonus { get; set; } = 0f;
    public DateTime LastActivityTime { get; set; } = DateTime.Now;
    
    // Events for stat changes
    public event Action<StatType, float, float> StatChanged; // StatType, oldValue, newValue
    public event Action<int> LevelUp; // New level
    
    public void SetStat(StatType statType, float value)
    {
        float oldValue = GetStat(statType);
        float clampedValue = ClampStat(statType, value);
        
        if (Math.Abs(oldValue - clampedValue) < 0.01f) return; // No significant change
        
        switch (statType)
        {
            case StatType.Experience:
                Experience = clampedValue;
                TotalExperience = Math.Max(TotalExperience, Experience);
                CheckLevelUp();
                break;
            case StatType.Hunger:
                Hunger = clampedValue;
                break;
            case StatType.Tiredness:
                Tiredness = clampedValue;
                break;
            case StatType.Comfort:
                Comfort = clampedValue;
                break;
            case StatType.Mood:
                Mood = clampedValue;
                break;
        }
        
        StatChanged?.Invoke(statType, oldValue, clampedValue);
    }
    
    public float GetStat(StatType statType)
    {
        return statType switch
        {
            StatType.Experience => Experience,
            StatType.Hunger => Hunger,
            StatType.Tiredness => Tiredness,
            StatType.Comfort => Comfort,
            StatType.Mood => Mood,
            _ => 0f
        };
    }
    
    public void AddExperience(float amount)
    {
        if (amount <= 0) return;
        
        float oldExp = Experience;
        Experience += amount;
        TotalExperience += amount;
        
        CheckLevelUp();
        StatChanged?.Invoke(StatType.Experience, oldExp, Experience);
    }
    
    private void CheckLevelUp()
    {
        while (Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            Level++;
            
            // Calculate next level requirement: 80 + (Level * 20)
            ExperienceToNext = 80f + (Level * 20f);
            
            LevelUp?.Invoke(Level);
        }
    }
    
    private float ClampStat(StatType statType, float value)
    {
        return statType switch
        {
            StatType.Experience => Math.Max(0f, value), // Experience can't be negative
            StatType.Hunger or StatType.Tiredness or StatType.Comfort or StatType.Mood => 
                Math.Clamp(value, 0f, 100f), // These stats are 0-100
            _ => value
        };
    }
    
    public void AddRecentActivityBonus(float bonus)
    {
        RecentActivityBonus = Math.Max(RecentActivityBonus, bonus);
        LastActivityTime = DateTime.Now;
    }
    
    public float GetDecayedActivityBonus()
    {
        // Activity bonus decays over time (5 minutes to fully decay)
        var timeSinceActivity = DateTime.Now - LastActivityTime;
        var decayFactor = Math.Max(0f, 1f - (float)(timeSinceActivity.TotalMinutes / 5.0));
        return RecentActivityBonus * decayFactor;
    }
}

public enum StatType
{
    Experience,
    Hunger,
    Tiredness,
    Comfort,
    Mood
}