using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class Adventurer
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Speed { get; set; } = 50f;
    
    private Texture2D _texture;
    private Rectangle _sourceRectangle;
    private Vector2 _direction;
    private float _directionChangeTimer;
    private float _directionChangeInterval = 3f; // Change direction every 3 seconds
    private Random _random;

    public Adventurer(Vector2 startPosition)
    {
        Position = startPosition;
        _direction = Vector2.UnitX; // Start moving right
        _random = new Random();
        _directionChangeTimer = 0f;
    }

    public void LoadContent(Texture2D texture)
    {
        _texture = texture;
        // Assuming a simple sprite for now - you can expand this for sprite sheets
        _sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update direction change timer
        _directionChangeTimer += deltaTime;
        
        // Occasionally change direction for natural exploration
        if (_directionChangeTimer >= _directionChangeInterval)
        {
            ChangeDirection();
            _directionChangeTimer = 0f;
        }
        
        // Update velocity and position
        Velocity = _direction * Speed;
        Position += Velocity * deltaTime;
    }

    private void ChangeDirection()
    {
        // Generate a new random direction
        float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
        _direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        
        // Vary the direction change interval for more natural movement
        _directionChangeInterval = 2f + (float)_random.NextDouble() * 4f;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_texture != null)
        {
            spriteBatch.Draw(_texture, Position, _sourceRectangle, Color.White);
        }
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, _sourceRectangle.Width, _sourceRectangle.Height);
    }
}