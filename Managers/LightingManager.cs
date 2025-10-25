using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

/// <summary>
/// Post-processing lighting manager
/// Renders lights to a separate texture and multiplies with the scene
/// </summary>
public class LightingManager : IDisposable
{
    private GraphicsDevice _graphicsDevice;
    private RenderTarget2D _lightMap;
    private Texture2D _lightTexture;
    private List<Light> _lights;
    
    // Ambient lighting
    public Color AmbientColor { get; set; }
    
    private int _screenWidth;
    private int _screenHeight;
    
    public LightingManager(GraphicsDevice graphicsDevice, int width, int height)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = width;
        _screenHeight = height;
        
        // Create lightmap render target
        _lightMap = new RenderTarget2D(graphicsDevice, width, height);
        
        // Create radial gradient texture for lights
        _lightTexture = CreateLightGradient(graphicsDevice, 128);
        
        _lights = new List<Light>();
        
        // Default ambient - full brightness for day
        AmbientColor = Color.White;
        
        Console.WriteLine("[LIGHTING] Post-processing lighting system initialized");
    }
    
    /// <summary>
    /// Creates a radial gradient texture for drawing lights
    /// </summary>
    private Texture2D CreateLightGradient(GraphicsDevice device, int size)
    {
        Texture2D texture = new Texture2D(device, size, size);
        Color[] data = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                float alpha = 1f - MathHelper.Clamp(dist / maxDist, 0f, 1f);
                
                // Smooth falloff
                alpha = alpha * alpha;
                
                data[y * size + x] = Color.White * alpha;
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    /// <summary>
    /// Add a light to the scene
    /// </summary>
    public Light AddLight(Vector2 position, Color color, float radius, float intensity = 1.0f)
    {
        var light = new Light(position, color, radius, intensity);
        _lights.Add(light);
        System.Console.WriteLine($"[LIGHTING] Added light at {position} - Total lights: {_lights.Count}");
        return light;
    }
    
    /// <summary>
    /// Remove a light from the scene
    /// </summary>
    public void RemoveLight(Light light)
    {
        bool removed = _lights.Remove(light);
        System.Console.WriteLine($"[LIGHTING] Removed light at {light.Position} - Success: {removed} - Total lights: {_lights.Count}");
    }
    
    /// <summary>
    /// Get all lights (read-only)
    /// </summary>
    public IReadOnlyList<Light> GetLights() => _lights.AsReadOnly();
    
    /// <summary>
    /// Get the light gradient texture for manual rendering
    /// </summary>
    public Texture2D GetLightTexture() => _lightTexture;
    
    /// <summary>
    /// Get the lightmap render target for multiply blending
    /// </summary>
    public RenderTarget2D GetLightMap() => _lightMap;
    
    /// <summary>
    /// Update all lights (for flickering, etc.)
    /// </summary>
    public void Update(GameTime gameTime)
    {
        foreach (var light in _lights)
        {
            if (light.Enabled)
            {
                light.Update(gameTime);
            }
        }
    }
    
    /// <summary>
    /// Render lights to the lightmap
    /// Call this AFTER drawing your scene
    /// </summary>
    public void BeginLightMap(Camera camera)
    {
        // Set render target to lightmap
        _graphicsDevice.SetRenderTarget(_lightMap);
        _graphicsDevice.Clear(AmbientColor); // Fill with ambient color
    }
    
    /// <summary>
    /// Draw all lights to the lightmap
    /// </summary>
    public void DrawLights(SpriteBatch spriteBatch, Camera camera)
    {
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.Additive, // Additive blending for lights
            SamplerState.LinearClamp,
            null,
            null,
            null,
            camera.GetTransformMatrix()
        );
        
        foreach (var light in _lights)
        {
            if (!light.Enabled) continue;
            
            // Calculate light size based on radius
            float scale = (light.Radius * 2f) / _lightTexture.Width;
            
            // Draw light as a textured quad
            spriteBatch.Draw(
                _lightTexture,
                light.Position,
                null,
                light.Color * light.Intensity,
                0f,
                new Vector2(_lightTexture.Width / 2f, _lightTexture.Height / 2f),
                scale,
                SpriteEffects.None,
                0f
            );
        }
        
        spriteBatch.End();
    }
    
    /// <summary>
    /// End lightmap rendering and restore default render target
    /// </summary>
    public void EndLightMap()
    {
        _graphicsDevice.SetRenderTarget(null);
    }
    
    /// <summary>
    /// Apply the lightmap to the screen using multiply blend
    /// </summary>
    public void ApplyLighting(SpriteBatch spriteBatch)
    {
        // Multiply blend: Result = Destination * Source
        // We want: FinalScene = Scene * Lightmap
        // This darkens areas without light and keeps lit areas bright
        var multiplyBlend = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,  // Use scene color
            ColorDestinationBlend = Blend.Zero,          // Don't add destination again
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero
        };

        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            multiplyBlend,
            SamplerState.LinearClamp,
            null,
            null,
            null,
            null
        );
        
        // Draw the lightmap over the scene
        spriteBatch.Draw(
            _lightMap,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            Color.White
        );
        
        spriteBatch.End();
    }
    
    public void Dispose()
    {
        _lightMap?.Dispose();
        _lightTexture?.Dispose();
    }
}
