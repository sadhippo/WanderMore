using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class BackgroundMusicManager
{
    private ContentManager _content;
    private AudioManager _audioManager;
    private TimeManager _timeManager;
    private WeatherManager _weatherManager;
    
    // Music tracks
    private Dictionary<string, Song> _musicTracks;
    private Song _currentTrack;
    private string _currentTrackName;
    
    // Playback state
    private bool _isPlaying;
    private bool _isFadingOut;
    private bool _isFadingIn;
    private float _fadeTimer;
    private float _fadeDuration = 2.0f; // 2 seconds for fade transitions
    private float _baseVolume = 0.35f; // Lower base volume for better integration
    
    // Track selection
    private List<string> _dayTracks;
    private List<string> _nightTracks;
    private Random _random;
    private int _lastDayTrackIndex = -1;
    private int _lastNightTrackIndex = -1;
    
    // Time-based effects
    private TimeOfDay _currentTimeOfDay;
    private float _volumeModifier = 1.0f;
    private bool _needsTrackChange;
    
    // Audio effects simulation (since MonoGame doesn't have built-in EQ)
    private float _nightVolumeReduction = 0.7f; // Night tracks play quieter
    private float _dawnDuskVolumeBoost = 1.0f; // Reduced boost for subtlety
    
    // Dynamic mixing variables
    private float _activityVolumeModifier = 1.0f; // Adjusts based on player activity
    private float _weatherVolumeModifier = 1.0f; // Adjusts based on weather
    private float _interactionVolumeModifier = 1.0f; // Reduces during interactions
    private float _movementVolumeModifier = 1.0f; // Adjusts based on movement
    
    // State tracking for dynamic mixing
    private bool _isPlayerMoving = false;
    private bool _isInInteraction = false;
    private float _lastMovementTime = 0f;
    private float _movementFadeTimer = 0f;
    private const float MOVEMENT_FADE_DURATION = 2.0f; // How long to fade after stopping movement
    
    public BackgroundMusicManager(ContentManager content, AudioManager audioManager, TimeManager timeManager, WeatherManager weatherManager = null)
    {
        _content = content;
        _audioManager = audioManager;
        _timeManager = timeManager;
        _weatherManager = weatherManager;
        _musicTracks = new Dictionary<string, Song>();
        _random = new Random();
        
        // Initialize track lists
        _dayTracks = new List<string> { "forest_day01", "forest_day02" };
        _nightTracks = new List<string> { "forest_night01" };
        
        _currentTimeOfDay = _timeManager.CurrentTimeOfDay;
        
        // Subscribe to time changes
        _timeManager.TimeOfDayChanged += OnTimeOfDayChanged;
        
        // Subscribe to volume changes
        _audioManager.OnMusicVolumeChanged += OnMusicVolumeChanged;
        
        // Subscribe to weather changes if available
        if (_weatherManager != null)
        {
            _weatherManager.WeatherChanged += OnWeatherChanged;
        }
    }
    
    public void LoadMusic()
    {
        try
        {
            // Load day tracks
            LoadTrack("forest_day01", "sounds/bgmusic/forest_day01");
            LoadTrack("forest_day02", "sounds/bgmusic/forest_day02");
            
            // Load night tracks
            LoadTrack("forest_night01", "sounds/bgmusic/forest_night01");
            
            System.Console.WriteLine($"[MUSIC] Loaded {_musicTracks.Count} background music tracks");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[MUSIC] Error loading music: {ex.Message}");
        }
    }
    
    private void LoadTrack(string trackName, string assetPath)
    {
        try
        {
            var song = _content.Load<Song>(assetPath);
            _musicTracks[trackName] = song;
            System.Console.WriteLine($"[MUSIC] Loaded track: {trackName}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[MUSIC] Failed to load track {trackName}: {ex.Message}");
        }
    }
    
    public void Update(float deltaTime)
    {
        // Handle fade transitions
        if (_isFadingOut || _isFadingIn)
        {
            _fadeTimer += deltaTime;
            float fadeProgress = _fadeTimer / _fadeDuration;
            
            if (_isFadingOut)
            {
                float volume = (1.0f - fadeProgress) * GetCurrentVolume();
                MediaPlayer.Volume = Math.Max(0.0f, volume);
                
                if (fadeProgress >= 1.0f)
                {
                    MediaPlayer.Stop();
                    _isFadingOut = false;
                    _isPlaying = false;
                    
                    // Start new track if needed
                    if (_needsTrackChange)
                    {
                        StartNewTrack();
                        _needsTrackChange = false;
                    }
                }
            }
            else if (_isFadingIn)
            {
                float volume = fadeProgress * GetCurrentVolume();
                MediaPlayer.Volume = Math.Min(GetCurrentVolume(), volume);
                
                if (fadeProgress >= 1.0f)
                {
                    _isFadingIn = false;
                    MediaPlayer.Volume = GetCurrentVolume();
                }
            }
        }
        else
        {
            // Update volume based on current settings (no fade)
            if (_isPlaying)
            {
                MediaPlayer.Volume = GetCurrentVolume();
            }
        }
        
        // Check if current track has ended and start a new one
        if (_isPlaying && MediaPlayer.State == MediaState.Stopped && !_isFadingOut && !_isFadingIn)
        {
            System.Console.WriteLine("[MUSIC] Track ended, starting new track");
            StartNewTrack();
        }
        
        // Apply time-based volume effects
        UpdateTimeBasedEffects(deltaTime);
    }
    
    private void UpdateTimeBasedEffects(float deltaTime)
    {
        // Apply sophisticated volume changes based on time of day progress
        float gameHour = _timeManager.CurrentGameHour;
        float timeProgress = 0f;
        
        // Calculate progress within current time period
        switch (_timeManager.CurrentTimeOfDay)
        {
            case TimeOfDay.Dawn:
                timeProgress = (gameHour - 5f) / 1f; // 5-6 AM
                break;
            case TimeOfDay.Day:
                timeProgress = (gameHour - 6f) / 12f; // 6 AM - 6 PM
                break;
            case TimeOfDay.Dusk:
                timeProgress = (gameHour - 18f) / 1f; // 6-7 PM
                break;
            case TimeOfDay.Night:
                if (gameHour >= 19f)
                    timeProgress = (gameHour - 19f) / 10f; // 7 PM - 5 AM
                else
                    timeProgress = (gameHour + 5f) / 10f; // Handle wrap-around
                break;
        }
        
        if (_timeManager.CurrentTimeOfDay == TimeOfDay.Day)
        {
            // Day: Dynamic volume based on sun position
            if (timeProgress < 0.15f) // Dawn (6:00-7:48 AM)
            {
                // Gradual sunrise - music gets brighter
                float dawnProgress = timeProgress / 0.15f;
                _volumeModifier = 0.6f + (dawnProgress * 0.5f); // 0.6 to 1.1
            }
            else if (timeProgress < 0.25f) // Morning (7:48-9:00 AM)
            {
                // Peak morning energy
                _volumeModifier = _dawnDuskVolumeBoost; // 1.1
            }
            else if (timeProgress < 0.75f) // Midday (9:00 AM-3:00 PM)
            {
                // Stable midday volume with slight variation
                float middayVariation = (float)Math.Sin(timeProgress * Math.PI * 2) * 0.05f;
                _volumeModifier = 1.0f + middayVariation; // 0.95 to 1.05
            }
            else if (timeProgress < 0.85f) // Evening (3:00-4:12 PM)
            {
                // Slight boost for evening energy
                _volumeModifier = _dawnDuskVolumeBoost; // 1.1
            }
            else // Dusk (4:12-6:00 PM)
            {
                // Gradual sunset - music gets softer
                float duskProgress = (timeProgress - 0.85f) / 0.15f;
                _volumeModifier = _dawnDuskVolumeBoost - (duskProgress * 0.4f); // 1.1 to 0.7
            }
        }
        else
        {
            // Night: Atmospheric and mysterious
            if (timeProgress < 0.2f) // Early night (6:00-8:24 PM)
            {
                // Transition into night atmosphere
                float earlyNightProgress = timeProgress / 0.2f;
                _volumeModifier = 0.7f - (earlyNightProgress * 0.1f); // 0.7 to 0.6
            }
            else if (timeProgress < 0.3f) // Evening (8:24-9:36 PM)
            {
                // Stable evening volume
                _volumeModifier = 0.6f * _nightVolumeReduction; // ~0.48
            }
            else if (timeProgress < 0.7f) // Deep night (9:36 PM-2:24 AM)
            {
                // Very quiet, mysterious atmosphere with subtle pulsing
                float nightPulse = (float)Math.Sin(timeProgress * Math.PI * 4) * 0.03f;
                _volumeModifier = (0.5f * _nightVolumeReduction) + nightPulse; // ~0.37-0.43
            }
            else if (timeProgress < 0.85f) // Late night (2:24-4:12 AM)
            {
                // Slightly louder as dawn approaches
                float lateNightProgress = (timeProgress - 0.7f) / 0.15f;
                _volumeModifier = (0.5f * _nightVolumeReduction) + (lateNightProgress * 0.15f); // ~0.4 to 0.52
            }
            else // Pre-dawn (4:12-6:00 AM)
            {
                // Building anticipation for sunrise
                float predawnProgress = (timeProgress - 0.85f) / 0.15f;
                _volumeModifier = 0.52f + (predawnProgress * 0.18f); // 0.52 to 0.7
            }
        }
        
        // Add subtle random variation to make it feel more organic
        float randomVariation = ((float)_random.NextDouble() - 0.5f) * 0.015f; // ±0.75% (reduced for subtlety)
        _volumeModifier = Math.Max(0.1f, Math.Min(1.2f, _volumeModifier + randomVariation));
        
        // Update weather-based volume modifier
        UpdateWeatherEffects();
        
        // Update movement-based volume modifier
        UpdateMovementEffects(deltaTime);
    }
    
    private float GetCurrentVolume()
    {
        if (_audioManager == null) return 0.0f;
        
        // Combine all volume modifiers for dynamic mixing
        float finalVolume = _baseVolume * 
                           _audioManager.MusicVolume * 
                           _audioManager.MasterVolume * 
                           _volumeModifier * 
                           _activityVolumeModifier * 
                           _weatherVolumeModifier * 
                           _interactionVolumeModifier * 
                           _movementVolumeModifier;
        
        return Math.Max(0.0f, Math.Min(1.0f, finalVolume));
    }
    
    private void OnTimeOfDayChanged(TimeOfDay newTimeOfDay)
    {
        System.Console.WriteLine($"[MUSIC] Time of day changed to {newTimeOfDay}");
        
        if (newTimeOfDay != _currentTimeOfDay)
        {
            _currentTimeOfDay = newTimeOfDay;
            
            // Fade out current track and prepare to start appropriate track for new time
            if (_isPlaying)
            {
                FadeOutCurrentTrack();
                _needsTrackChange = true;
            }
            else
            {
                // Start immediately if no music is playing
                StartNewTrack();
            }
        }
    }
    
    private void OnMusicVolumeChanged(float newVolume)
    {
        // Immediately update the volume when user changes it
        RefreshVolume();
        System.Console.WriteLine($"[MUSIC] Volume updated to {GetCurrentVolume():F2} (base: {newVolume:F2})");
    }
    
    private void OnWeatherChanged(WeatherType newWeather)
    {
        System.Console.WriteLine($"[MUSIC] Weather changed to {newWeather}, adjusting audio");
    }
    
    private void UpdateWeatherEffects()
    {
        if (_weatherManager == null)
        {
            _weatherVolumeModifier = 1.0f;
            return;
        }
        
        // Adjust volume based on weather conditions
        switch (_weatherManager.CurrentWeather)
        {
            case WeatherType.Clear:
                _weatherVolumeModifier = 1.0f; // Normal volume
                break;
                
            case WeatherType.Cloudy:
                _weatherVolumeModifier = 0.9f; // Slightly muffled
                break;
                
            case WeatherType.Rain:
                // Rain competes with music, so lower it more
                float rainIntensity = _weatherManager.WeatherIntensity;
                _weatherVolumeModifier = 0.6f - (rainIntensity * 0.2f); // 0.4-0.6 range
                break;
                
            case WeatherType.Snow:
                _weatherVolumeModifier = 0.8f; // Muffled by snow
                break;
                
            case WeatherType.Fog:
                _weatherVolumeModifier = 0.7f; // Atmospheric dampening
                break;
                
            default:
                _weatherVolumeModifier = 1.0f;
                break;
        }
    }
    
    private void UpdateMovementEffects(float deltaTime)
    {
        // Update movement fade timer
        if (!_isPlayerMoving && _movementFadeTimer > 0f)
        {
            _movementFadeTimer -= deltaTime;
            
            // Fade from movement volume back to idle volume
            float fadeProgress = 1.0f - (_movementFadeTimer / MOVEMENT_FADE_DURATION);
            _movementVolumeModifier = MathHelper.Lerp(1.1f, 1.0f, fadeProgress);
        }
        else if (_isPlayerMoving)
        {
            // Slightly boost volume during movement for energy
            _movementVolumeModifier = 1.1f;
            _movementFadeTimer = MOVEMENT_FADE_DURATION;
        }
        else
        {
            _movementVolumeModifier = 1.0f;
        }
    }
    
    // Methods to be called by game systems to inform music manager of state changes
    public void SetPlayerMoving(bool isMoving)
    {
        if (_isPlayerMoving != isMoving)
        {
            _isPlayerMoving = isMoving;
            if (isMoving)
            {
                _lastMovementTime = 0f; // Reset movement timer
            }
        }
    }
    
    public void SetInInteraction(bool inInteraction)
    {
        if (_isInInteraction != inInteraction)
        {
            _isInInteraction = inInteraction;
            _interactionVolumeModifier = inInteraction ? 0.4f : 1.0f; // Much quieter during interactions
            
            if (inInteraction)
            {
                System.Console.WriteLine("[MUSIC] Lowering volume for interaction");
            }
            else
            {
                System.Console.WriteLine("[MUSIC] Restoring volume after interaction");
            }
        }
    }
    
    public void SetActivityLevel(float activityLevel)
    {
        // Activity level from 0.0 (idle) to 1.0 (very active)
        // Slightly boost music during high activity
        _activityVolumeModifier = 0.8f + (activityLevel * 0.3f); // 0.8 to 1.1 range
    }
    
    public void StartMusic()
    {
        if (!_isPlaying && _musicTracks.Count > 0)
        {
            StartNewTrack();
        }
    }
    
    public void StopMusic()
    {
        if (_isPlaying)
        {
            FadeOutCurrentTrack();
        }
    }
    
    private void StartNewTrack()
    {
        string trackName = SelectAppropriateTrack();
        
        if (!string.IsNullOrEmpty(trackName) && _musicTracks.ContainsKey(trackName))
        {
            _currentTrack = _musicTracks[trackName];
            _currentTrackName = trackName;
            
            try
            {
                MediaPlayer.Play(_currentTrack);
                MediaPlayer.IsRepeating = false; // We'll handle track changes manually
                MediaPlayer.Volume = 0.0f; // Start silent for fade in
                
                _isPlaying = true;
                _isFadingIn = true;
                _fadeTimer = 0.0f;
                
                System.Console.WriteLine($"[MUSIC] Started track: {trackName}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[MUSIC] Error starting track {trackName}: {ex.Message}");
            }
        }
    }
    
    private void FadeOutCurrentTrack()
    {
        if (_isPlaying && !_isFadingOut)
        {
            _isFadingOut = true;
            _isFadingIn = false;
            _fadeTimer = 0.0f;
            System.Console.WriteLine($"[MUSIC] Fading out track: {_currentTrackName}");
        }
    }
    
    private string SelectAppropriateTrack()
    {
        List<string> availableTracks = _currentTimeOfDay == TimeOfDay.Day ? _dayTracks : _nightTracks;
        
        if (availableTracks.Count == 0)
        {
            System.Console.WriteLine($"[MUSIC] No tracks available for {_currentTimeOfDay}");
            return null;
        }
        
        if (availableTracks.Count == 1)
        {
            return availableTracks[0];
        }
        
        // Select a different track than the last one played
        int trackIndex;
        int lastIndex = _currentTimeOfDay == TimeOfDay.Day ? _lastDayTrackIndex : _lastNightTrackIndex;
        
        do
        {
            trackIndex = _random.Next(availableTracks.Count);
        }
        while (trackIndex == lastIndex && availableTracks.Count > 1);
        
        // Update last played index
        if (_currentTimeOfDay == TimeOfDay.Day)
        {
            _lastDayTrackIndex = trackIndex;
        }
        else
        {
            _lastNightTrackIndex = trackIndex;
        }
        
        return availableTracks[trackIndex];
    }
    
    public void SetBaseVolume(float volume)
    {
        _baseVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
        System.Console.WriteLine($"[MUSIC] Set base volume to {_baseVolume:F2}");
    }
    
    public void SetFadeDuration(float duration)
    {
        _fadeDuration = Math.Max(0.1f, duration);
    }
    
    // Add new tracks dynamically
    public void AddDayTrack(string trackName, string assetPath)
    {
        LoadTrack(trackName, assetPath);
        if (_musicTracks.ContainsKey(trackName) && !_dayTracks.Contains(trackName))
        {
            _dayTracks.Add(trackName);
            System.Console.WriteLine($"[MUSIC] Added day track: {trackName}");
        }
    }
    
    public void AddNightTrack(string trackName, string assetPath)
    {
        LoadTrack(trackName, assetPath);
        if (_musicTracks.ContainsKey(trackName) && !_nightTracks.Contains(trackName))
        {
            _nightTracks.Add(trackName);
            System.Console.WriteLine($"[MUSIC] Added night track: {trackName}");
        }
    }
    
    public bool IsPlaying => _isPlaying;
    public string CurrentTrackName => _currentTrackName;
    public float CurrentVolume => GetCurrentVolume();
    
    // Method to immediately update volume (useful when user changes settings)
    public void RefreshVolume()
    {
        if (_isPlaying && !_isFadingIn && !_isFadingOut)
        {
            MediaPlayer.Volume = GetCurrentVolume();
        }
    }
    
    public void Dispose()
    {
        if (_timeManager != null)
        {
            _timeManager.TimeOfDayChanged -= OnTimeOfDayChanged;
        }
        
        if (_audioManager != null)
        {
            _audioManager.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        }
        
        if (_weatherManager != null)
        {
            _weatherManager.WeatherChanged -= OnWeatherChanged;
        }
        
        MediaPlayer.Stop();
        
        foreach (var track in _musicTracks.Values)
        {
            track?.Dispose();
        }
        _musicTracks.Clear();
    }
}