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
    public float Speed { get; set; } = 80f;
    
    private Texture2D _idleTexture;
    private Texture2D _walkingTexture;
    private Rectangle _sourceRectangle;
    private Vector2 _direction;
    private float _directionChangeTimer;
    private float _directionChangeInterval = 3f; // Change direction every 3 seconds
    private Random _random;
    
    // Animation system
    private Dictionary<AnimationType, AnimationData> _animations;
    private AnimationType _currentAnimation;
    private int _currentFrame;
    private float _animationTimer;
    private bool _isMoving;

    public Adventurer(Vector2 startPosition)
    {
        Position = startPosition;
        _direction = Vector2.UnitX; // Start moving right
        _random = new Random();
        _directionChangeTimer = 0f;
    }

    public void LoadContent(Texture2D idleTexture, Texture2D walkingTexture)
    {
        _idleTexture = idleTexture;
        _walkingTexture = walkingTexture;
        
        // Initialize animation system
        SetupAnimations();
        
        // Start with idle animation
        PlayAnimation(AnimationType.Idle);
    }

    private void SetupAnimations()
    {
        _animations = new Dictionary<AnimationType, AnimationData>
        {
            [AnimationType.Idle] = new AnimationData
            {
                Texture = _idleTexture,
                FrameCount = 1,
                FrameTime = 1.0f, // Doesn't matter for single frame
                IsLooping = true,
                FrameWidth = 32,
                FrameHeight = 32
            },
            [AnimationType.Walking] = new AnimationData
            {
                Texture = _walkingTexture,
                FrameCount = 3,
                FrameTime = 0.2f, // 200ms per frame
                IsLooping = true,
                FrameWidth = 32,
                FrameHeight = 32
            }
        };
    }

    private void PlayAnimation(AnimationType animationType)
    {
        if (_currentAnimation != animationType)
        {
            _currentAnimation = animationType;
            _currentFrame = 0;
            _animationTimer = 0f;
        }
    }

    public void Update(GameTime gameTime, ZoneManager zoneManager, PoIManager poiManager = null)
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
        
        // Calculate potential new position
        Velocity = _direction * Speed;
        Vector2 newPosition = Position + Velocity * deltaTime;
        
        // Check if we're moving (for animation)
        _isMoving = Velocity.Length() > 0.1f;
        
        // Update animation
        UpdateAnimation(deltaTime);
        
        // Check for collision and update position
        bool terrainCollision = CheckCollision(newPosition, zoneManager);
        bool poiCollision = poiManager != null && CheckPoICollision(newPosition, poiManager);
        
        if (terrainCollision || poiCollision)
        {
            // Check if we can transition to another zone (only for terrain collision)
            if (terrainCollision && zoneManager.TryTransitionZone(newPosition, out Vector2 transitionPosition))
            {
                // Successfully transitioned to new zone
                Position = transitionPosition;
            }
            else
            {
                // Collision detected - bounce off or change direction
                HandleCollision(zoneManager);
            }
        }
        else
        {
            // No collision - move normally
            Position = newPosition;
        }
    }

    private void ChangeDirection()
    {
        // Generate a new random direction
        float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
        _direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        
        // Vary the direction change interval for more natural movement
        _directionChangeInterval = 2f + (float)_random.NextDouble() * 4f;
    }

    private void UpdateAnimation(float deltaTime)
    {
        // Determine which animation should be playing
        AnimationType targetAnimation = _isMoving ? AnimationType.Walking : AnimationType.Idle;
        PlayAnimation(targetAnimation);
        
        // Update current animation
        var currentAnimData = _animations[_currentAnimation];
        
        if (currentAnimData.FrameCount > 1)
        {
            _animationTimer += deltaTime;
            
            if (_animationTimer >= currentAnimData.FrameTime)
            {
                if (currentAnimData.IsLooping)
                {
                    _currentFrame = (_currentFrame + 1) % currentAnimData.FrameCount;
                }
                else
                {
                    _currentFrame = Math.Min(_currentFrame + 1, currentAnimData.FrameCount - 1);
                }
                _animationTimer = 0f;
            }
        }
        
        // Update source rectangle based on current frame
        _sourceRectangle = new Rectangle(
            _currentFrame * currentAnimData.FrameWidth, 
            0, 
            currentAnimData.FrameWidth, 
            currentAnimData.FrameHeight
        );
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var currentAnimData = _animations[_currentAnimation];
        
        if (currentAnimData.Texture != null)
        {
            // Determine sprite effects based on movement direction
            SpriteEffects effects = SpriteEffects.None;
            
            // Flip sprite horizontally if moving left
            if (_direction.X < -0.1f)
            {
                effects = SpriteEffects.FlipHorizontally;
            }
            
            spriteBatch.Draw(
                currentAnimData.Texture, 
                Position, 
                _sourceRectangle, 
                Color.White, 
                0f, 
                Vector2.Zero, 
                1f, 
                effects, 
                0f
            );
        }
    }

    // Public method to trigger specific animations (for future use)
    public void TriggerAnimation(AnimationType animationType)
    {
        PlayAnimation(animationType);
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, _sourceRectangle.Width, _sourceRectangle.Height);
    }

    private bool CheckCollision(Vector2 newPosition, ZoneManager zoneManager)
    {
        // Check collision at multiple points around the adventurer's bounds
        int spriteSize = 32;
        
        // Check corners and center of the sprite
        Vector2[] checkPoints = {
            newPosition, // Top-left
            new Vector2(newPosition.X + spriteSize - 1, newPosition.Y), // Top-right
            new Vector2(newPosition.X, newPosition.Y + spriteSize - 1), // Bottom-left
            new Vector2(newPosition.X + spriteSize - 1, newPosition.Y + spriteSize - 1), // Bottom-right
            new Vector2(newPosition.X + spriteSize / 2, newPosition.Y + spriteSize / 2) // Center
        };

        foreach (var point in checkPoints)
        {
            if (IsPositionSolid(point, zoneManager))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPositionSolid(Vector2 position, ZoneManager zoneManager)
    {
        // Check if position is outside zone bounds
        if (!zoneManager.IsPositionInBounds(position))
        {
            return true; // Treat zone boundaries as solid
        }
        
        var terrainType = zoneManager.GetTerrainAt(position);
        
        // Return true if terrain is solid (stone or water)
        return terrainType == TerrainType.Stone || terrainType == TerrainType.Water;
    }

    private void HandleCollision(ZoneManager zoneManager)
    {
        // Try to bounce off the obstacle
        Vector2 originalDirection = _direction;
        
        // Try different bounce directions
        Vector2[] bounceDirections = {
            new Vector2(-_direction.X, _direction.Y), // Reflect X
            new Vector2(_direction.X, -_direction.Y), // Reflect Y
            new Vector2(-_direction.X, -_direction.Y), // Reflect both
            Vector2.Transform(_direction, Matrix.CreateRotationZ(MathHelper.PiOver2)), // Rotate 90 degrees
            Vector2.Transform(_direction, Matrix.CreateRotationZ(-MathHelper.PiOver2)), // Rotate -90 degrees
        };

        // Test each bounce direction
        foreach (var bounceDir in bounceDirections)
        {
            _direction = Vector2.Normalize(bounceDir);
            Vector2 testPosition = Position + _direction * Speed * 0.1f; // Small test movement
            
            if (!CheckCollision(testPosition, zoneManager))
            {
                // Found a valid direction, reset timer for more natural movement
                _directionChangeTimer = 0f;
                _directionChangeInterval = 1f + (float)_random.NextDouble() * 2f; // Shorter interval after collision
                return;
            }
        }
        
        // If no bounce direction works, pick a completely random direction
        ChangeDirection();
    }

    private bool CheckPoICollision(Vector2 newPosition, PoIManager poiManager)
    {
        // Check collision with PoIs (buildings and large objects)
        var nearbyPoIs = poiManager.GetNearbyPoIs(newPosition, 64f);
        
        Rectangle adventurerBounds = new Rectangle(
            (int)newPosition.X, (int)newPosition.Y, 32, 32
        );
        
        foreach (var poi in nearbyPoIs)
        {
            // Only collide with buildings and large objects
            if (IsCollidablePoI(poi.Type) && adventurerBounds.Intersects(poi.GetBounds()))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsCollidablePoI(PoIType type)
    {
        // Buildings and large structures should block movement
        return type switch
        {
            PoIType.Farmhouse or PoIType.Inn or PoIType.Cottage or PoIType.Hut or
            PoIType.Castle or PoIType.Chapel or PoIType.Oracle or PoIType.SkullFortress or
            PoIType.HauntedHouse or PoIType.TreeHouse or PoIType.Mine => true,
            _ => false // NPCs and animals don't block movement
        };
    }
}

public enum AnimationType
{
    Idle,
    Walking,
    Celebrating,    // Future: when discovering something cool
    Resting,        // Future: occasional rest animation
    Surprised,      // Future: when encountering obstacles
    Waving          // Future: greeting animation
}

public class AnimationData
{
    public Texture2D Texture { get; set; }
    public int FrameCount { get; set; }
    public float FrameTime { get; set; }
    public bool IsLooping { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
}