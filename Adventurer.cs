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
    
    // Stuck detection
    private Vector2 _lastPosition;
    private float _stuckTimer;
    private float _stuckThreshold = 2f; // Consider stuck after 2 seconds (increased due to interaction pauses)
    
    // PoI interaction system
    private bool _isInteracting;
    private float _interactionTimer;
    private float _interactionDuration;
    private PointOfInterest _currentInteractionPoI;
    private PointOfInterest _lastInteractionPoI;
    private float _interactionCooldownTimer;
    private float _interactionCooldownDuration = 5f; // 5 seconds before can interact with same PoI again
    
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
        
        // Update interaction cooldown
        if (_interactionCooldownTimer > 0)
        {
            _interactionCooldownTimer -= deltaTime;
        }
        
        // Handle PoI interaction
        if (_isInteracting)
        {
            _interactionTimer += deltaTime;
            
            if (_interactionTimer >= _interactionDuration)
            {
                // Finish interaction
                _lastInteractionPoI = _currentInteractionPoI;
                _interactionCooldownTimer = _interactionCooldownDuration;
                _isInteracting = false;
                _interactionTimer = 0f;
                
                // Move away from the PoI after interaction
                if (poiManager != null)
                {
                    MoveAwayFromPoI();
                }
                
                _currentInteractionPoI = null; // Clear after moving away
                System.Console.WriteLine("Homeboy finished interacting and is moving on!");
            }
            else
            {
                // Stay still during interaction
                Velocity = Vector2.Zero;
                _isMoving = false;
                UpdateAnimation(deltaTime);
                return; // Skip movement logic while interacting
            }
        }
        
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
        
        // Check if stuck and handle it
        CheckStuckState(deltaTime, zoneManager, poiManager);
        
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
            else if (poiCollision && poiManager != null)
            {
                // Check if we should interact with a PoI
                var nearbyPoI = GetNearestInteractablePoI(poiManager);
                if (nearbyPoI != null && !_isInteracting)
                {
                    StartInteraction(nearbyPoI);
                }
                else
                {
                    // No interaction or already interacting - handle as normal collision
                    HandleCollision(zoneManager, poiManager);
                }
            }
            else
            {
                // Terrain collision - bounce off or change direction
                HandleCollision(zoneManager, poiManager);
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

    private void CheckStuckState(float deltaTime, ZoneManager zoneManager, PoIManager poiManager)
    {
        // Don't check for stuck state while interacting with PoIs
        if (_isInteracting)
        {
            _stuckTimer = 0f; // Reset stuck timer during interactions
            _lastPosition = Position;
            return;
        }
        
        // Check if adventurer has moved significantly
        float distanceMoved = Vector2.Distance(Position, _lastPosition);
        
        if (distanceMoved < 3f) // Less than 3 pixels moved (more sensitive)
        {
            _stuckTimer += deltaTime;
            
            if (_stuckTimer >= _stuckThreshold)
            {
                // Force a new direction when stuck
                ForceNewDirection(zoneManager, poiManager);
                _stuckTimer = 0f;
            }
        }
        else
        {
            _stuckTimer = 0f; // Reset stuck timer if moving
        }
        
        _lastPosition = Position;
    }

    private void ForceNewDirection(ZoneManager zoneManager = null, PoIManager poiManager = null)
    {
        System.Console.WriteLine("Homeboy is stuck! Trying to find a new path...");
        
        // Try to find a completely clear direction
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle = _random.NextSingle() * MathHelper.TwoPi;
            Vector2 testDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            
            // Test if this direction is clear for a longer distance
            bool isClear = true;
            for (int step = 1; step <= 5; step++)
            {
                Vector2 stepPosition = Position + testDirection * Speed * 0.2f * step;
                
                // Check terrain collision
                if (zoneManager != null && CheckCollision(stepPosition, zoneManager))
                {
                    isClear = false;
                    break;
                }
                
                // Check PoI collision
                if (poiManager != null && CheckPoICollision(stepPosition, poiManager))
                {
                    isClear = false;
                    break;
                }
            }
            
            if (isClear)
            {
                _direction = testDirection;
                _directionChangeTimer = 0f;
                _directionChangeInterval = 0.3f; // Short interval after being stuck
                
                System.Console.WriteLine("Homeboy found a clear path and got unstuck!");
                return;
            }
        }
        
        // If still no clear path, try moving away from the nearest obstacle
        Vector2 escapeDirection = FindEscapeDirection(zoneManager, poiManager);
        _direction = escapeDirection;
        _directionChangeTimer = 0f;
        _directionChangeInterval = 0.2f;
        
        System.Console.WriteLine("Homeboy is trying to escape from obstacles!");
    }

    private Vector2 FindEscapeDirection(ZoneManager zoneManager, PoIManager poiManager)
    {
        // Find the direction away from the nearest obstacles
        Vector2 escapeDirection = Vector2.Zero;
        
        // Check for nearby PoIs and move away from them
        if (poiManager != null)
        {
            var nearbyPoIs = poiManager.GetNearbyPoIs(Position, 100f);
            foreach (var poi in nearbyPoIs)
            {
                if (IsCollidablePoI(poi.Type))
                {
                    Vector2 awayFromPoI = Position - poi.Position;
                    if (awayFromPoI.Length() > 0)
                    {
                        awayFromPoI.Normalize();
                        escapeDirection += awayFromPoI;
                    }
                }
            }
        }
        
        // If no clear escape direction from PoIs, try random directions
        if (escapeDirection.Length() < 0.1f)
        {
            float angle = _random.NextSingle() * MathHelper.TwoPi;
            escapeDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
        else
        {
            escapeDirection.Normalize();
        }
        
        return escapeDirection;
    }

    private void MoveAwayFromPoI()
    {
        if (_currentInteractionPoI != null)
        {
            // Calculate direction away from the PoI
            Vector2 awayDirection = Position - _currentInteractionPoI.Position;
            if (awayDirection.Length() > 0)
            {
                awayDirection.Normalize();
                
                // Add some randomness to avoid getting stuck in a straight line
                float randomAngle = (_random.NextSingle() - 0.5f) * MathHelper.PiOver2; // ±45 degrees
                Matrix rotation = Matrix.CreateRotationZ(randomAngle);
                awayDirection = Vector2.Transform(awayDirection, rotation);
                awayDirection.Normalize();
                
                _direction = awayDirection;
                _directionChangeTimer = 0f;
                _directionChangeInterval = 2f + _random.NextSingle() * 3f; // Longer interval after interaction
                
                System.Console.WriteLine($"Homeboy is moving away from {_currentInteractionPoI.Type} in direction {_direction}");
            }
            else
            {
                // If we're exactly on top of the PoI, pick a random direction
                float angle = _random.NextSingle() * MathHelper.TwoPi;
                _direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                _directionChangeTimer = 0f;
                _directionChangeInterval = 2f + _random.NextSingle() * 3f;
                
                System.Console.WriteLine($"Homeboy was on top of {_currentInteractionPoI.Type}, picking random direction");
            }
        }
    }

    private void StartInteraction(PointOfInterest poi)
    {
        _isInteracting = true;
        _interactionTimer = 0f;
        _interactionDuration = 2f + _random.NextSingle() * 2f; // 2-4 seconds interaction
        _currentInteractionPoI = poi;
        
        string interactionType = GetInteractionDescription(poi.Type);
        System.Console.WriteLine($"Homeboy is {interactionType} for {_interactionDuration:F1} seconds!");
    }

    private string GetInteractionDescription(PoIType poiType)
    {
        return poiType switch
        {
            // Buildings
            PoIType.Inn => "having a drink at the inn",
            PoIType.Cottage => "visiting the cottage",
            PoIType.Farmhouse => "checking out the farmhouse",
            PoIType.Castle => "admiring the castle",
            PoIType.Chapel => "praying at the chapel",
            PoIType.Hut => "exploring the hut",
            PoIType.Mine => "investigating the mine",
            
            // NPCs
            PoIType.Ranger => "chatting with the ranger",
            PoIType.Priest => "speaking with the priest",
            PoIType.Warrior => "talking to the warrior",
            PoIType.Scholar => "learning from the scholar",
            PoIType.Hermit => "visiting the hermit",
            PoIType.Adventurer => "meeting a fellow adventurer",
            PoIType.Mermaid => "watching the mermaid",
            
            // Animals
            PoIType.Cat => "petting the cat",
            PoIType.Dog => "playing with the dog",
            PoIType.Unicorn => "admiring the unicorn",
            PoIType.Sheep => "watching the sheep",
            PoIType.Chicken => "observing the chickens",
            PoIType.Pig => "looking at the pig",
            PoIType.Deer => "watching the deer",
            
            _ => "examining something interesting"
        };
    }

    private PointOfInterest GetNearestInteractablePoI(PoIManager poiManager)
    {
        var nearbyPoIs = poiManager.GetNearbyPoIs(Position, 40f);
        PointOfInterest nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var poi in nearbyPoIs)
        {
            // Only interact with certain types of PoIs
            if (IsInteractablePoI(poi.Type))
            {
                // Skip if this is the same PoI we just interacted with and cooldown is active
                if (_lastInteractionPoI != null && poi.Id == _lastInteractionPoI.Id && _interactionCooldownTimer > 0)
                {
                    continue;
                }
                
                float distance = Vector2.Distance(Position, poi.Position);
                if (distance < nearestDistance)
                {
                    nearest = poi;
                    nearestDistance = distance;
                }
            }
        }
        
        return nearest;
    }

    private bool IsInteractablePoI(PoIType poiType)
    {
        return poiType switch
        {
            // Buildings
            PoIType.Inn => true,
            PoIType.Cottage => true,
            PoIType.Farmhouse => true,
            PoIType.Castle => true,
            PoIType.Chapel => true,
            PoIType.Hut => true,
            PoIType.Mine => true,
            
            // NPCs
            PoIType.Ranger => true,
            PoIType.Priest => true,
            PoIType.Warrior => true,
            PoIType.Scholar => true,
            PoIType.Hermit => true,
            PoIType.Adventurer => true,
            PoIType.Mermaid => true,
            
            // Animals
            PoIType.Cat => true,
            PoIType.Dog => true,
            PoIType.Unicorn => true,
            PoIType.Sheep => true,
            PoIType.Chicken => true,
            PoIType.Pig => true,
            PoIType.Deer => true,
            
            // Don't interact with monsters or dangerous things
            PoIType.Skeleton => false,
            PoIType.Dragon => false,
            PoIType.Minotaur => false,
            PoIType.Golem => false,
            
            _ => false
        };
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

    private void HandleCollision(ZoneManager zoneManager, PoIManager poiManager = null)
    {
        // Debug: Check what we're colliding with
        if (poiManager != null)
        {
            var nearbyPoIs = poiManager.GetNearbyPoIs(Position, 50f);
            foreach (var poi in nearbyPoIs)
            {
                if (IsCollidablePoI(poi.Type) && Vector2.Distance(Position, poi.Position) < 40f)
                {
                    System.Console.WriteLine($"Homeboy collided with {poi.Type} at distance {Vector2.Distance(Position, poi.Position):F1}");
                }
            }
        }
        
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
            
            bool terrainCollision = CheckCollision(testPosition, zoneManager);
            bool poiCollision = poiManager != null && CheckPoICollision(testPosition, poiManager);
            
            if (!terrainCollision && !poiCollision)
            {
                // Found a valid direction, reset timer for more natural movement
                _directionChangeTimer = 0f;
                _directionChangeInterval = 1f + (float)_random.NextDouble() * 2f; // Shorter interval after collision
                return;
            }
        }
        
        // If no bounce direction works, try more directions around the obstacle
        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8f) * MathHelper.TwoPi;
            _direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 testPosition = Position + _direction * Speed * 0.2f; // Slightly larger test movement
            
            bool terrainCollision = CheckCollision(testPosition, zoneManager);
            bool poiCollision = poiManager != null && CheckPoICollision(testPosition, poiManager);
            
            if (!terrainCollision && !poiCollision)
            {
                _directionChangeTimer = 0f;
                _directionChangeInterval = 0.5f + (float)_random.NextDouble() * 1f; // Even shorter interval
                return;
            }
        }
        
        // Last resort: pick a completely random direction
        ChangeDirection();
    }

    private bool CheckPoICollision(Vector2 newPosition, PoIManager poiManager)
    {
        // Check collision with PoIs (buildings and large objects)
        var nearbyPoIs = poiManager.GetNearbyPoIs(newPosition, 64f);
        
        // Make adventurer bounds slightly smaller for more forgiving collision
        Rectangle adventurerBounds = new Rectangle(
            (int)newPosition.X + 4, (int)newPosition.Y + 4, 24, 24
        );
        
        foreach (var poi in nearbyPoIs)
        {
            // Only collide with buildings and large objects
            if (IsCollidablePoI(poi.Type))
            {
                // Make PoI bounds slightly smaller too for easier navigation
                Rectangle poiBounds = new Rectangle(
                    (int)poi.Position.X + 2, (int)poi.Position.Y + 2, 28, 28
                );
                
                if (adventurerBounds.Intersects(poiBounds))
                {
                    return true;
                }
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