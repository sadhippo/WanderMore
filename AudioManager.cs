using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class AudioManager
{
    private ContentManager _content;
    private Dictionary<string, SoundEffect[]> _soundGroups;
    private Dictionary<string, float> _lastPlayTimes; // Prevent sound spam
    private float _totalGameTime; // Track total game time for cooldowns
    private Random _random;
    
    // Volume controls
    public float MasterVolume { get; set; } = 1.0f;
    public float SfxVolume { get; set; } = 0.7f;
    
    // Sound cooldowns (in seconds)
    private const float FOOTSTEP_COOLDOWN = 0.2f; // Reduced for more responsive footsteps
    
    public AudioManager(ContentManager content)
    {
        _content = content;
        _soundGroups = new Dictionary<string, SoundEffect[]>();
        _lastPlayTimes = new Dictionary<string, float>();
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
            
            // Future expansion: Load other terrain footsteps
            // LoadSoundGroup("footsteps_grass", "sounds/steps_grass", 4);
            // LoadSoundGroup("footsteps_stone", "sounds/steps_stone", 4);
            // LoadSoundGroup("footsteps_water", "sounds/steps_water", 4);
            
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
    
    public void Update(float deltaTime)
    {
        _totalGameTime += deltaTime;
    }
    
    public void PlayRandomSound(string groupName, float volume = 1.0f, float pitch = 0.0f, float pan = 0.0f, float cooldown = 0f)
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
        
        // Clamp values using Math.Max/Min instead of MathHelper
        float finalVolume = Math.Max(0.0f, Math.Min(1.0f, volume * SfxVolume * MasterVolume));
        float finalPitch = Math.Max(-1.0f, Math.Min(1.0f, pitch));
        float finalPan = Math.Max(-1.0f, Math.Min(1.0f, pan));
        
        randomSound.Play(finalVolume, finalPitch, finalPan);
        
        // Update last play time using game time
        _lastPlayTimes[groupName] = _totalGameTime;
    }
    
    public void PlayFootstep(float volume = 1.0f, string terrainType = "dirt")
    {
        // Add slight random pitch variation for more natural footsteps
        float pitchVariation = (float)(_random.NextDouble() - 0.5) * 0.2f; // ±0.1 pitch variation
        
        // Try terrain-specific footsteps first, fall back to default
        string soundGroup = $"footsteps_{terrainType}";
        if (!_soundGroups.ContainsKey(soundGroup))
        {
            soundGroup = "footsteps"; // Default fallback
        }
        
        PlayRandomSound(soundGroup, volume, pitchVariation, 0f, FOOTSTEP_COOLDOWN);
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
