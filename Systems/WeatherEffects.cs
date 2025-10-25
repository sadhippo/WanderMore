using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class WeatherEffects
{
    private List<RainDrop> _rainDrops;
    private List<SnowFlake> _snowFlakes;
    private Texture2D _pixelTexture;
    private Random _random;
    private Camera _camera;
    private Rectangle _screenBounds;

    public WeatherEffects(GraphicsDevice graphicsDevice, Camera camera)
    {
        _camera = camera;
        _rainDrops = new List<RainDrop>();
        _snowFlakes = new List<SnowFlake>();
        _random = new Random();
        
        // Create pixel texture for particles
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        _screenBounds = new Rectangle(0, 0, 1024, 768); // Default screen size
    }

    public void UpdateScreenBounds(Rectangle bounds)
    {
        _screenBounds = bounds;
    }

    public void Update(GameTime gameTime, WeatherType weather, float intensity)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        switch (weather)
        {
            case WeatherType.Rain:
                UpdateRain(deltaTime, intensity);
                break;
            case WeatherType.Snow:
                UpdateSnow(deltaTime, intensity);
                break;
            default:
                // Clear weather - remove particles
                _rainDrops.Clear();
                _snowFlakes.Clear();
                break;
        }
    }

    private void UpdateRain(float deltaTime, float intensity)
    {
        // Spawn new raindrops
        int spawnRate = (int)(intensity * 150); // 0-150 drops per second
        for (int i = 0; i < spawnRate * deltaTime; i++)
        {
            if (_rainDrops.Count < 300) // Max 300 drops
            {
                SpawnRainDrop();
            }
        }

        // Update existing raindrops
        for (int i = _rainDrops.Count - 1; i >= 0; i--)
        {
            var drop = _rainDrops[i];
            drop.Position.Y += drop.Speed * deltaTime;
            drop.Position.X += drop.Wind * deltaTime;
            
            // Remove drops that fall off screen
            if (drop.Position.Y > _screenBounds.Height + 50)
            {
                _rainDrops.RemoveAt(i);
            }
        }
    }

    private void UpdateSnow(float deltaTime, float intensity)
    {
        // Spawn new snowflakes
        int spawnRate = (int)(intensity * 80); // 0-80 flakes per second
        for (int i = 0; i < spawnRate * deltaTime; i++)
        {
            if (_snowFlakes.Count < 200) // Max 200 flakes
            {
                SpawnSnowFlake();
            }
        }

        // Update existing snowflakes
        for (int i = _snowFlakes.Count - 1; i >= 0; i--)
        {
            var flake = _snowFlakes[i];
            flake.Position.Y += flake.Speed * deltaTime;
            flake.Position.X += MathF.Sin(flake.Position.Y * 0.01f + flake.Wobble) * 20f * deltaTime;
            
            // Remove flakes that fall off screen
            if (flake.Position.Y > _screenBounds.Height + 50)
            {
                _snowFlakes.RemoveAt(i);
            }
        }
    }

    private void SpawnRainDrop()
    {
        var drop = new RainDrop
        {
            Position = new Vector2(
                _random.Next(-100, _screenBounds.Width + 100),
                -10
            ),
            Speed = 300 + _random.Next(-50, 100), // 250-400 pixels per second
            Wind = _random.Next(-30, 10), // Slight wind effect
            Length = _random.Next(3, 8),
            Alpha = 0.6f + _random.NextSingle() * 0.4f
        };
        _rainDrops.Add(drop);
    }

    private void SpawnSnowFlake()
    {
        var flake = new SnowFlake
        {
            Position = new Vector2(
                _random.Next(-50, _screenBounds.Width + 50),
                -10
            ),
            Speed = 50 + _random.Next(-20, 40), // 30-90 pixels per second
            Size = _random.Next(1, 4),
            Wobble = _random.NextSingle() * MathHelper.TwoPi,
            Alpha = 0.7f + _random.NextSingle() * 0.3f
        };
        _snowFlakes.Add(flake);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw rain
        foreach (var drop in _rainDrops)
        {
            Color dropColor = Color.LightBlue * drop.Alpha;
            
            // Draw rain as small vertical lines
            Rectangle dropRect = new Rectangle(
                (int)drop.Position.X,
                (int)drop.Position.Y,
                1,
                drop.Length
            );
            spriteBatch.Draw(_pixelTexture, dropRect, dropColor);
        }

        // Draw snow
        foreach (var flake in _snowFlakes)
        {
            Color flakeColor = Color.White * flake.Alpha;
            
            // Draw snow as small squares/circles
            Rectangle flakeRect = new Rectangle(
                (int)flake.Position.X,
                (int)flake.Position.Y,
                flake.Size,
                flake.Size
            );
            spriteBatch.Draw(_pixelTexture, flakeRect, flakeColor);
        }
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}

public class RainDrop
{
    public Vector2 Position;
    public float Speed;
    public float Wind;
    public int Length;
    public float Alpha;
}

public class SnowFlake
{
    public Vector2 Position;
    public float Speed;
    public int Size;
    public float Wobble;
    public float Alpha;
}