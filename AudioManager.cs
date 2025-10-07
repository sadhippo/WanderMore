using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public enum AudioCategory
{
    Music,
    SFX,
    Ambient
}

public class AudioManager
{
    private ContentManager _content;
    private Dictionary<string, SoundEffect[]> _soundGroups;
    private Dictionary<string, float> _lastPlayTimes; // Prevent sound spam
    private Dictionary<string, SoundEffectInstance> _loopingSounds; // For ambient/weather sounds
    private float _totalGameTime; // Track total game time for cooldowns
    private Random _random;
    
    // Volume controls
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 0.9f; // Increased for better footstep audibility
    public float AmbientVolume { get; set; } = 0.6f;
    
    // Default values for reset functionality
    public const float DEFAULT_MASTER_VOLUME = 1.0f;
    public const float DEFAULT_MUSIC_VOLUME = 0.8f;
    public const float DEFAULT_SFX_VOLUME = 0.9f;
    public const float DEFAULT_AMBIENT_VOLUME = 0.6f;
    
    // Sound cooldowns (in seconds)
    private const float FOOTSTEP_COOLDOWN = 0.2f; // Reduced for more responsive footsteps
    
    public AudioManager(ContentManager content)
    {
        _content = content;
        _soundGroups = new Dictionary<string, SoundEffect[]>();
        _lastPlayTimes = new Dictionary<string, float>();
        _loopingSounds = new Dictionary<string, SoundEffectInstance>();
        _random = new Random();
    }
    
    public void LoadSounds()
    {
        try
        {
            // Load footstep sounds - dirt (default)
            LoadSoundGroup("footsteps_dirt", "sounds/Steps_dirt", 4);
            
            // Set default footsteps to dirt for now
            if (_soundGroups.ContainsKey("footsteps_dirt"))
            {
                _soundGroups["footsteps"] = _soundGroups["footsteps_dirt"];
                System.Console.WriteLine("[AUDIO] Set default footsteps to dirt");
            }
            
            // Load weather sounds
            LoadSingleSound("rain_light", "sounds/Rain_light-01");
            
            // Future expansion: Load other terrain footsteps
            // LoadSoundGroup("footsteps_grass", "sounds/steps_grass", 4);
            // LoadSoundGroup("footsteps_stone", "sounds/steps_stone", 4);
            // LoadSoundGroup("footsteps_water", "sounds/steps_water", 4);
            
            // Future expansion: Load other weather sounds
            // LoadSingleSound("rain_heavy", "sounds/Rain_heavy-01");
            // LoadSingleSound("wind", "sounds/Wind-01");
            // LoadSingleSound("thunder", "sounds/Thunder-01");
            
            System.Console.WriteLine($"[AUDIO] Audio loading complete. {_soundGroups.Count} sound groups loaded.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[AUDIO] Error loading sounds: {ex.Message}");
        }
    }
    
    private void LoadSoundGroup(string groupName, string basePath, int count)
    {
        var sounds = new List<SoundEffect>();
        
        for (int i = 1; i <= count; i++)
        {
            try
            {
                var sound = _content.Load<SoundEffect>($"{basePath}-{i:D2}");
                sounds.Add(sound);
                System.Console.WriteLine($"[AUDIO] Loaded {groupName}: {basePath}-{i:D2}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AUDIO] Failed to load {basePath}-{i:D2}: {ex.Message}");
            }
        }
        
        if (sounds.Count > 0)
        {
            _soundGroups[groupName] = sounds.ToArray();
            System.Console.WriteLine($"[AUDIO] Loaded {sounds.Count} sounds for group '{groupName}'");
        }
    }
    
    private void LoadSingleSound(string soundName, string soundPath)
    {
        try
        {
            var sound = _content.Load<SoundEffect>(soundPath);
            _soundGroups[soundName] = new SoundEffect[] { sound };
            System.Console.WriteLine($"[AUDIO] Loaded single sound '{soundName}': {soundPath}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[AUDIO] Failed to load {soundPath}: {ex.Message}");
        }
    }
    
    public void Update(float deltaTime)
    {
        _totalGameTime += deltaTime;
    }
    
    public void PlayRandomSound(string groupName, float volume = 1.0f, float pitch = 0.0f, float pan = 0.0f, float cooldown = 0f, AudioCategory category = AudioCategory.SFX)
    {
        if (!_soundGroups.ContainsKey(groupName) || _soundGroups[groupName].Length == 0)
            return;
            
        // Check cooldown using game time
        if (cooldown > 0f && _lastPlayTimes.ContainsKey(groupName))
        {
            float timeSinceLastPlay = _totalGameTime - _lastPlayTimes[groupName];
            if (timeSinceLastPlay < cooldown)
                return;
        }
        
        var sounds = _soundGroups[groupName];
        var randomSound = sounds[_random.Next(sounds.Length)];
        
        // Apply category-specific volume
        float categoryVolume = GetCategoryVolume(category);
        float finalVolume = Math.Max(0.0f, Math.Min(1.0f, volume * categoryVolume * MasterVolume));
        float finalPitch = Math.Max(-1.0f, Math.Min(1.0f, pitch));
        float finalPan = Math.Max(-1.0f, Math.Min(1.0f, pan));
        
        randomSound.Play(finalVolume, finalPitch, finalPan);
        
        // Update last play time using game time
        _lastPlayTimes[groupName] = _totalGameTime;
    }
    
    private float GetCategoryVolume(AudioCategory category)
    {
        return category switch
        {
            AudioCategory.Music => MusicVolume,
            AudioCategory.SFX => SfxVolume,
            AudioCategory.Ambient => AmbientVolume,
            _ => SfxVolume
        };
    }
    
    public void PlayFootstep(float volume = 1.0f, string terrainType = "dirt")
    {
        // Add slight random pitch variation for more natural footsteps
        float pitchVariation = (float)(_random.NextDouble() - 0.5) * 0.2f; // ±0.1 pitch variation
        
        // Try terrain-specific footsteps first, then fallbacks
        string soundGroup = $"footsteps_{terrainType}";
        if (!_soundGroups.ContainsKey(soundGroup))
        {
            soundGroup = "footsteps"; // Default fallback
            if (!_soundGroups.ContainsKey(soundGroup))
            {
                soundGroup = "footsteps_dirt"; // Direct fallback to dirt
            }
        }
        
        PlayRandomSound(soundGroup, volume, pitchVariation, 0f, FOOTSTEP_COOLDOWN, AudioCategory.SFX);
    }
    
    public void StartLoopingSound(string soundName, float volume = 0.4f, AudioCategory category = AudioCategory.Ambient)
    {
        if (!_soundGroups.ContainsKey(soundName) || _soundGroups[soundName].Length == 0)
        {
            System.Console.WriteLine($"[AUDIO] Sound '{soundName}' not found for looping");
            return;
        }
        
        // Stop existing instance if playing
        StopLoopingSound(soundName);
        
        // Create new looping instance
        var soundEffect = _soundGroups[soundName][0]; // Use first sound in group
        var instance = soundEffect.CreateInstance();
        
        float categoryVolume = GetCategoryVolume(category);
        float finalVolume = Math.Max(0.0f, Math.Min(1.0f, volume * categoryVolume * MasterVolume));
        instance.Volume = finalVolume;
        instance.IsLooped = true;
        instance.Play();
        
        _loopingSounds[soundName] = instance;
        System.Console.WriteLine($"[AUDIO] Started looping sound '{soundName}' at volume {finalVolume:F2}");
    }
    
    public void StopLoopingSound(string soundName)
    {
        if (_loopingSounds.ContainsKey(soundName))
        {
            var instance = _loopingSounds[soundName];
            instance.Stop();
            instance.Dispose();
            _loopingSounds.Remove(soundName);
            System.Console.WriteLine($"[AUDIO] Stopped looping sound '{soundName}'");
        }
    }
    
    public void UpdateLoopingVolume(string soundName, float volume)
    {
        if (_loopingSounds.ContainsKey(soundName))
        {
            float finalVolume = Math.Max(0.0f, Math.Min(1.0f, volume * SfxVolume * MasterVolume));
            _loopingSounds[soundName].Volume = finalVolume;
        }
    }
    
    public void StartRainSound(float intensity = 0.5f)
    {
        // Scale volume based on rain intensity (0.0 to 1.0)
        float volume = Math.Max(0.1f, Math.Min(0.4f, intensity * 0.4f));
        StartLoopingSound("rain_light", volume, AudioCategory.Ambient);
    }
    
    public void StopRainSound()
    {
        StopLoopingSound("rain_light");
    }
    
    public void UpdateRainVolume(float intensity)
    {
        float volume = Math.Max(0.1f, Math.Min(0.4f, intensity * 0.4f));
        UpdateLoopingVolume("rain_light", volume);
    }
    
    // Volume control methods for UI
    public void SetMasterVolume(float volume)
    {
        MasterVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
        RefreshAllLoopingVolumes();
    }
    
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
        RefreshAllLoopingVolumes();
    }
    
    public void SetSfxVolume(float volume)
    {
        SfxVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
        RefreshAllLoopingVolumes();
    }
    
    public void SetAmbientVolume(float volume)
    {
        AmbientVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
        RefreshAllLoopingVolumes();
    }
    
    public void ResetToDefaults()
    {
        MasterVolume = DEFAULT_MASTER_VOLUME;
        MusicVolume = DEFAULT_MUSIC_VOLUME;
        SfxVolume = DEFAULT_SFX_VOLUME;
        AmbientVolume = DEFAULT_AMBIENT_VOLUME;
        RefreshAllLoopingVolumes();
        System.Console.WriteLine("[AUDIO] Reset all volumes to defaults");
    }
    
    private void RefreshAllLoopingVolumes()
    {
        // Update all currently playing looping sounds with new volume settings
        foreach (var kvp in _loopingSounds)
        {
            var soundName = kvp.Key;
            var instance = kvp.Value;
            
            // Determine category and base volume for this sound
            AudioCategory category = soundName.Contains("rain") ? AudioCategory.Ambient : AudioCategory.SFX;
            float baseVolume = soundName.Contains("rain") ? 0.4f : 0.6f; // Default volumes
            
            float categoryVolume = GetCategoryVolume(category);
            float finalVolume = Math.Max(0.0f, Math.Min(1.0f, baseVolume * categoryVolume * MasterVolume));
            instance.Volume = finalVolume;
        }
    }
    
    // Future expansion methods
    public void SetFootstepTerrain(string terrainType)
    {
        string soundGroup = $"footsteps_{terrainType}";
        if (_soundGroups.ContainsKey(soundGroup))
        {
            _soundGroups["footsteps"] = _soundGroups[soundGroup];
            System.Console.WriteLine($"[AUDIO] Switched footsteps to {terrainType}");
        }
    }
    
    public bool HasSoundGroup(string groupName)
    {
        return _soundGroups.ContainsKey(groupName) && _soundGroups[groupName].Length > 0;
    }
    
    public void Dispose()
    {
        // Stop and dispose all looping sounds
        foreach (var instance in _loopingSounds.Values)
        {
            instance?.Stop();
            instance?.Dispose();
        }
        _loopingSounds.Clear();
        
        // Dispose sound effects
        foreach (var soundGroup in _soundGroups.Values)
        {
            foreach (var sound in soundGroup)
            {
                sound?.Dispose();
            }
        }
        _soundGroups.Clear();
    }
}
