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
    public bool IsSleeping => _isSleeping;
    
    private Texture2D _idleTexture;
    private Texture2D _walkingTexture; // legacy ref
    private Texture2D _sleepingTexture;
    private Texture2D _walkingTextureDay;
    private Texture2D _walkingTextureNight;
    private bool _isNightMode;
    private Vector2 _direction;
    private float _directionChangeTimer;
    private float _directionChangeInterval = 3f; // Change direction every 3 seconds
    private Random _random;
    
    // Stuck detection
    private Vector2 _lastPosition;
    private float _stuckTimer;
    private float _stuckThreshold = 5f; // Consider stuck after 5 seconds (much more forgiving)
    
    // Building collision detection
    private int _buildingCollisionCount;
    private const int MAX_BUILDING_COLLISIONS = 3; // Force direction change after 3 consecutive collisions
    private float _collisionResetTimer;
    private const float COLLISION_RESET_TIME = 1f; // Reset counter if no collision for 1 second
    
    // PoI interaction system
    private bool _isInteracting;
    private float _interactionTimer;
    private float _interactionDuration;
    private PointOfInterest _currentInteractionPoI;
    private PointOfInterest _lastInteractionPoI;
    private float _interactionCooldownTimer;
    private float _interactionCooldownDuration = 5f; // 5 seconds before can interact with same PoI again
    // Pathfinding system
    private PathfindingManager _pathfindingManager;
    private bool _isPathfinding;
    
    // Stats system integration
    private StatsManager _statsManager;
    
    // Audio system integration
    private AudioManager _audioManager;
    private float _footstepTimer;
    private const float FOOTSTEP_INTERVAL = 0.6f; // Play footstep every 0.6 seconds while walking (relaxed pace)
    
    // Lighting system
    private LightingManager _lightingManager;
    private Light _lanternLight;
    private const float LANTERN_NORMAL_INTENSITY = 1.0f; // Higher intensity for alpha blending
    private const float LANTERN_NORMAL_RADIUS = 180f; // Good visibility radius
    private const float CAMPFIRE_INTENSITY = 1.5f; // Brighter when sleeping (campfire)
    private const float CAMPFIRE_RADIUS = 240f; // Larger radius for campfire
    
    // Sleeping system
    private bool _isSleeping;
    private float _sleepTimer;
    private const float SLEEP_DURATION = 30f; // Sleep for 30 seconds
    private const float TIREDNESS_THRESHOLD = 50f; // Start sleeping when tiredness drops below 50 (higher for testing)
    
    // Animation system
    private Dictionary<AnimationType, AnimationData> _animations;
    private AnimationType _currentAnimation;
    private int _currentFrame;
    private float _animationTimer;
    private bool _isMoving;
    private Rectangle _sourceRectangle;

    public Adventurer(Vector2 startPosition)
    {
        Position = startPosition;
        _direction = Vector2.UnitX; // Start moving right
        _random = new Random();
        _directionChangeTimer = 0f;
        
        // Initialize pathfinding system
        _pathfindingManager = new PathfindingManager();
        _isPathfinding = false;
        System.Console.WriteLine("[ADVENTURER] Pathfinding system initialized");
    }

    public void LoadTorchTexture(Texture2D torchTexture)
    {
        _walkingTextureNight = torchTexture;
        // Refresh animations to pick the correct texture if already in night mode
        SetupAnimations();
    }

    public void SetNightMode(bool needsLight)
    {
        if (_isNightMode == needsLight) return;
        _isNightMode = needsLight;
        
        System.Console.WriteLine($"[ADVENTURER] Light mode changed to: {needsLight}, Night texture available: {_walkingTextureNight != null}");
        
        // Control lantern light visibility
        if (_lanternLight != null)
        {
            _lanternLight.Enabled = needsLight;
            System.Console.WriteLine($"[ADVENTURER] Lantern light enabled: {_lanternLight.Enabled}");
        }
        
        // Swap walking texture in-place to avoid rebuilding everything
        if (_animations != null && _animations.ContainsKey(AnimationType.Walking))
        {
            var anim = _animations[AnimationType.Walking];
            anim.Texture = (_isNightMode && _walkingTextureNight != null) ? _walkingTextureNight : _walkingTextureDay;
            // Update frame count dynamically (torchboy_small has 4 frames, day walking has 3)
            if (anim.Texture != null)
            {
                anim.FrameCount = Math.Max(1, anim.Texture.Width / 32);
                System.Console.WriteLine($"[ADVENTURER] Walking animation updated: FrameCount={anim.FrameCount}, TextureWidth={anim.Texture.Width}");
            }
            _animations[AnimationType.Walking] = anim;
        }
    }
    
    public void SetStatsManager(StatsManager statsManager)
    {
        _statsManager = statsManager;
    }
    
    public void SetAudioManager(AudioManager audioManager)
    {
        _audioManager = audioManager;
        System.Console.WriteLine("[ADVENTURER] AudioManager connected");
    }
    
    public void SetLightingManager(LightingManager lightingManager)
    {
        _lightingManager = lightingManager;
        
        // Create and add the adventurer's lantern light
        if (_lightingManager != null)
        {
            _lanternLight = _lightingManager.AddLight(Position, LightPresets.Lantern, LANTERN_NORMAL_RADIUS, LANTERN_NORMAL_INTENSITY);
            System.Console.WriteLine($"[ADVENTURER] Lantern light created: Radius={LANTERN_NORMAL_RADIUS}, Intensity={LANTERN_NORMAL_INTENSITY}, Enabled={_lanternLight?.Enabled}");
        }
        else
        {
            System.Console.WriteLine("[ADVENTURER] WARNING: LightingManager is null!");
        }
    }

    public void LoadContent(Texture2D idleTexture, Texture2D walkingTexture)
    {
        _idleTexture = idleTexture;
        _walkingTexture = walkingTexture;
        _walkingTextureDay = walkingTexture;
        
        // Try to load sleeping texture separately
        // This will be handled by a separate method
        
        // Initialize animation system
        SetupAnimations();
        
        // Start with idle animation
        PlayAnimation(AnimationType.Idle);
    }
    
    public void LoadSleepingTexture(Texture2D sleepingTexture)
    {
        _sleepingTexture = sleepingTexture;
        
        // Re-setup animations to include sleeping
        SetupAnimations();
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
                Texture = (_isNightMode && _walkingTextureNight != null) ? _walkingTextureNight : _walkingTextureDay,
                FrameCount = ((_isNightMode && _walkingTextureNight != null) ? _walkingTextureNight.Width : _walkingTextureDay?.Width ?? 32) / 32,
                FrameTime = 0.2f, // 200ms per frame
                IsLooping = true,
                FrameWidth = 32,
                FrameHeight = 32
            }
        };
        
        // Add sleeping animation if texture is available
        if (_sleepingTexture != null)
        {
            _animations[AnimationType.Sleeping] = new AnimationData
            {
                Texture = _sleepingTexture,
                FrameCount = 4,
                FrameTime = 0.5f, // 500ms per frame for slower, peaceful animation
                IsLooping = true,
                FrameWidth = 32,
                FrameHeight = 32
            };
        }
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

    public void Update(GameTime gameTime, ZoneManager zoneManager, PoIManager poiManager = null, QuestManager questManager = null)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update lantern light position
        if (_lanternLight != null)
        {
            _lanternLight.Position = Position + new Vector2(16, 16); // Center of sprite
        }
        
        // Update collision reset timer
        _collisionResetTimer += deltaTime;
        if (_collisionResetTimer >= COLLISION_RESET_TIME)
        {
            // Reset building collision counter if no collision for a while
            if (_buildingCollisionCount > 0)
            {
                _buildingCollisionCount = 0;
            }
        }
        
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
                // Trigger interaction event for quest system
                if (poiManager != null && _currentInteractionPoI != null)
                {
                    // This will trigger the PoIInteracted event that the QuestManager subscribes to
                    var interactionResult = _currentInteractionPoI.Interact(this);
                    System.Console.WriteLine($"[INTERACTION] {interactionResult.Message}");
                    
                    // Fire the interaction event through the PoI manager
                    poiManager.TriggerInteraction(_currentInteractionPoI, this);
                    System.Console.WriteLine($"[INTERACTION] Triggered interaction event for {_currentInteractionPoI.Type}");
                }
                
                // Finish interaction
                _lastInteractionPoI = _currentInteractionPoI;
                _interactionCooldownTimer = _interactionCooldownDuration;
                
                // PoI interaction is now permanently marked as interacted via HasBeenInteracted flag
                System.Console.WriteLine($"[INTERACTION] {_currentInteractionPoI.Type} marked as interacted (permanent)");
                
                _isInteracting = false;
                _interactionTimer = 0f;
                
                // Move away from the PoI after interaction
                if (poiManager != null)
                {
                    MoveAwayFromPoI();
                }
                
                _currentInteractionPoI = null; // Clear after moving away
                System.Console.WriteLine("[INTERACTION] Finished interacting and moving on!");
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
        
        // Handle sleeping system
        UpdateSleepingBehavior(deltaTime);
        
        // Skip movement if sleeping
        if (_isSleeping)
        {
            // Update animation while sleeping
            UpdateAnimation(deltaTime);
            return;
        }
        
        // Update pathfinding system
        if (PathfindingConfig.PATHFINDING_ENABLED)
        {
            _pathfindingManager.Update(Position, zoneManager, poiManager, deltaTime, questManager);
            
            // Check if pathfinding reached a target and we should start interaction
            if (_pathfindingManager.CurrentTarget != null && 
                _pathfindingManager.CurrentState == PathfindingManager.PathfindingState.Wandering &&
                !_isInteracting)
            {
                // Pathfinding reached target, check if we should interact
                float distanceToTarget = Vector2.Distance(Position, _pathfindingManager.CurrentTarget.Position);
                if (distanceToTarget <= _pathfindingManager.CurrentTarget.InteractionRange)
                {
                    var targetPoI = _pathfindingManager.CurrentTarget;
                    if (IsInteractablePoI(targetPoI.Type))
                    {
                        StartInteraction(targetPoI);
                        _pathfindingManager.AbandonCurrentTarget(); // Now abandon after starting interaction
                    }
                }
            }
            
            // Get direction from pathfinding system
            Vector2 pathfindingDirection = _pathfindingManager.GetNextDirection(Position, _direction);
            _isPathfinding = _pathfindingManager.HasActiveTarget;
            
            if (_isPathfinding)
            {
                _direction = pathfindingDirection;
                // Reset wandering timer when pathfinding
                _directionChangeTimer = 0f;
            }
        }
        
        // Update direction change timer (only for wandering)
        if (!_isPathfinding)
        {
            _directionChangeTimer += deltaTime;
            
            // Occasionally change direction for natural exploration
            if (_directionChangeTimer >= _directionChangeInterval)
            {
                ChangeDirection();
                _directionChangeTimer = 0f;
            }
        }
        
        // Calculate potential new position
        Velocity = _direction * Speed;
        Vector2 newPosition = Position + Velocity * deltaTime;
        
        // Check if we're moving (for animation)
        _isMoving = Velocity.Length() > 0.1f;
        
        // Update footstep timer and play sounds
        if (_isMoving && _audioManager != null)
        {
            _footstepTimer += deltaTime;
            if (_footstepTimer >= FOOTSTEP_INTERVAL)
            {
                _audioManager.PlayFootstep(0.8f); // Good audible volume
                _footstepTimer = 0f;
            }
        }
        else
        {
            // Reset timer when not moving
            _footstepTimer = 0f;
        }
        
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
                // Increment building collision counter and reset timer
                _buildingCollisionCount++;
                _collisionResetTimer = 0f;
                
                // Check if we've hit too many consecutive collisions
                if (_buildingCollisionCount >= MAX_BUILDING_COLLISIONS)
                {
                    System.Console.WriteLine($"[COLLISION] Too many building collisions ({_buildingCollisionCount}), forcing direction change and abandoning pathfinding");
                    _pathfindingManager.AbandonCurrentTarget();
                    ForceNewDirection(zoneManager, poiManager);
                    _buildingCollisionCount = 0; // Reset after forcing change
                }
                else
                {
                    // Check if we should interact with a PoI
                    var nearbyPoI = GetNearestInteractablePoI(poiManager);
                    if (nearbyPoI != null && !_isInteracting)
                    {
                        StartInteraction(nearbyPoI);
                        // Clear pathfinding to prevent immediately going back after interaction
                        _pathfindingManager.AbandonCurrentTarget();
                        _buildingCollisionCount = 0; // Reset on interaction
                    }
                    else
                    {
                        // No interaction or already interacting - handle as normal collision
                        HandleCollision(zoneManager, poiManager);
                    }
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
            // No collision - move normally and reset collision counter
            Vector2 oldPosition = Position;
            Position = newPosition;
            
            // Track movement for stats system
            if (_statsManager != null)
            {
                float distanceMoved = Vector2.Distance(oldPosition, newPosition);
                _statsManager.OnMovement(distanceMoved);
            }
            
            // No need to manually reset here since the timer handles it
        }
    }

    private void UpdateSleepingBehavior(float deltaTime)
    {
        if (_statsManager == null) return;
        
        float currentTiredness = _statsManager.CurrentStats.Tiredness;
        
        // Check if we should start sleeping
        if (!_isSleeping && currentTiredness < TIREDNESS_THRESHOLD)
        {
            System.Console.WriteLine($"[SLEEP DEBUG] Tiredness {currentTiredness:F1} < {TIREDNESS_THRESHOLD}, starting sleep");
            StartSleeping();
        }
        
        // Update sleep timer if sleeping
        if (_isSleeping)
        {
            _sleepTimer += deltaTime;
            
            // Regenerate tiredness while sleeping - much faster rate
            float regenRate = 100f / SLEEP_DURATION; // Regenerate to full in SLEEP_DURATION seconds
            float regenAmount = regenRate * deltaTime;
            
            // Make regeneration much more noticeable - multiply by 10
            regenAmount *= 10f;
            float oldTiredness = _statsManager.CurrentStats.Tiredness;
            _statsManager.RegenerateTiredness(regenAmount);
            
            // Get updated tiredness after regeneration
            float updatedTiredness = _statsManager.CurrentStats.Tiredness;
            
            // Debug output every few seconds
            if ((int)_sleepTimer % 5 == 0 && _sleepTimer > 0)
            {
                System.Console.WriteLine($"[SLEEP DEBUG] Sleeping for {_sleepTimer:F1}s, Tiredness: {oldTiredness:F1} -> {updatedTiredness:F1} (+{regenAmount:F2})");
            }
            
            // Check if we're fully rested or have slept long enough
            if (updatedTiredness >= 90f || _sleepTimer >= SLEEP_DURATION)
            {
                StopSleeping();
            }
        }
    }
    
    private void StartSleeping()
    {
        _isSleeping = true;
        _sleepTimer = 0f;
        
        // Stop all movement
        Velocity = Vector2.Zero;
        _direction = Vector2.Zero;
        
        // Make lantern brighter and larger (campfire effect)
        if (_lanternLight != null)
        {
            _lanternLight.Intensity = CAMPFIRE_INTENSITY;
            _lanternLight.Radius = CAMPFIRE_RADIUS;
            _lanternLight.Color = LightPresets.Campfire; // Warmer campfire color
            _lanternLight.Flickers = true; // Add flickering for campfire effect
            System.Console.WriteLine("[ADVENTURER] Campfire lit - settling in for sleep");
        }
        
        // Animation will be handled by UpdateAnimation method
        
        System.Console.WriteLine("[ADVENTURER] Started sleeping - too tired to continue");
        
        // Add journal entry about sleeping
        if (_statsManager != null)
        {
            _statsManager.OnSleepStarted();
        }
    }
    
    private void StopSleeping()
    {
        _isSleeping = false;
        _sleepTimer = 0f;
        
        // Restore lantern to normal settings when waking up
        if (_lanternLight != null)
        {
            _lanternLight.Intensity = LANTERN_NORMAL_INTENSITY;
            _lanternLight.Radius = LANTERN_NORMAL_RADIUS;
            _lanternLight.Color = LightPresets.Lantern; // Back to lantern color
            _lanternLight.Flickers = false; // Stop flickering
            System.Console.WriteLine("[ADVENTURER] Campfire extinguished, lantern restored");
        }
        
        // Animation will be handled by UpdateAnimation method
        
        System.Console.WriteLine("[ADVENTURER] Woke up refreshed and ready to continue exploring");
        
        // Add journal entry about waking up
        if (_statsManager != null)
        {
            _statsManager.OnSleepEnded();
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
        AnimationType targetAnimation;
        
        if (_isSleeping)
        {
            // Use sleeping animation if available, otherwise use idle
            targetAnimation = _animations.ContainsKey(AnimationType.Sleeping) ? AnimationType.Sleeping : AnimationType.Idle;
        }
        else
        {
            targetAnimation = _isMoving ? AnimationType.Walking : AnimationType.Idle;
        }
        
        // Only switch animation if it changed
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
        
        if (distanceMoved < 1f) // Less than 1 pixel moved (less sensitive)
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
        System.Console.WriteLine("[ADVENTURER] Stuck detected, finding new direction...");
        
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
                
                System.Console.WriteLine("[ADVENTURER] Found clear path, unstuck!");
                return;
            }
        }
        
        // If still no clear path, try moving away from the nearest obstacle
        Vector2 escapeDirection = FindEscapeDirection(zoneManager, poiManager);
        _direction = escapeDirection;
        _directionChangeTimer = 0f;
        _directionChangeInterval = 0.2f;
        
        System.Console.WriteLine("[ADVENTURER] Escaping from obstacles...");
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
        _interactionDuration = 3f + _random.NextSingle() * 3f; // 3-6 seconds interaction (longer for visibility)
        _currentInteractionPoI = poi;
        
        string interactionType = GetInteractionDescription(poi.Type);
        System.Console.WriteLine($"[INTERACTION] Starting: {interactionType} for {_interactionDuration:F1} seconds!");
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
            
            // Resources
            PoIType.BerryBush => true,
            
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
    
    // Pathfinding public interface
    public void SetQuestTarget(PointOfInterest target)
    {
        if (PathfindingConfig.PATHFINDING_ENABLED && _pathfindingManager != null)
        {
            _pathfindingManager.SetQuestTarget(target);
        }
    }
    
    public void AbandonCurrentTarget()
    {
        if (_pathfindingManager != null)
        {
            _pathfindingManager.AbandonCurrentTarget();
        }
    }
    
    public bool IsPathfinding => _isPathfinding;
    
    public string GetPathfindingStatus()
    {
        if (_pathfindingManager != null)
        {
            return _pathfindingManager.GetPathfindingStatus();
        }
        return "Pathfinding Disabled";
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
    Sleeping,       // Sleeping animation when tired
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