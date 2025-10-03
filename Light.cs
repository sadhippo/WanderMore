using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

/// <summary>
/// Represents a light source in the game world
/// </summary>
public class Light
{
    public Guid Id { get; private set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public float Radius { get; set; }
    public float Intensity { get; set; }
    public bool Enabled { get; set; }
    
    // Optional: Flickering
    public bool Flickers { get; set; }
    private float _flickerTimer;
    private float _baseIntensity;
    private Random _random;
    
    public Light(Vector2 position, Color color, float radius, float intensity = 1.0f)
    {
        Id = Guid.NewGuid();
        Position = position;
        Color = color;
        Radius = radius;
        Intensity = intensity;
        Enabled = true;
        Flickers = false;
        
        _baseIntensity = intensity;
        _random = new Random(Id.GetHashCode());
    }
    
    public void Update(GameTime gameTime)
    {
        if (!Flickers) return;
        
        // Simple flicker animation
        _flickerTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 3f;
        float flicker = (float)Math.Sin(_flickerTimer) * 0.1f + (_random.NextSingle() - 0.5f) * 0.05f;
        Intensity = _baseIntensity * (0.7f + flicker);
    }
}

/// <summary>
/// Preset light configurations for common use cases
/// </summary>
public static class LightPresets
{
    // Colors
    public static readonly Color Torch = new Color(255, 180, 100);
    public static readonly Color Campfire = new Color(255, 140, 60);
    public static readonly Color Lantern = new Color(255, 240, 180);
    public static readonly Color Moonlight = new Color(200, 220, 255);
    public static readonly Color WindowLight = new Color(255, 200, 140);
    
    // Factory methods
    public static Light CreateTorch(Vector2 position)
    {
        return new Light(position, Torch, 220f, .4f) { Flickers = true };
    }
    
    public static Light CreateCampfire(Vector2 position)
    {
        return new Light(position, Campfire, 120f, 1.5f) { Flickers = true };
    }
    
    public static Light CreateLantern(Vector2 position)
    {
        return new Light(position, Lantern, 100f, 1.0f);
    }
    
    public static Light CreateWindowLight(Vector2 position)
    {
        return new Light(position, WindowLight, 60f, 0.8f);
    }
}
