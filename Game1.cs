using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    // Game systems
    private AssetManager _assetManager;
    private AudioManager _audioManager;
    private Adventurer _adventurer;
    private Camera _camera;
    private ZoneManager _zoneManager;
    private MiniMap _miniMap;
    private TimeManager _timeManager;
    private WeatherManager _weatherManager;
    private WeatherEffects _weatherEffects;
    private JournalManager _journalManager;
    private JournalUI _journalUI;
    private PoIManager _poiManager;
    private QuestManager _questManager;
    private UIManager _uiManager;
    private StatsManager _statsManager;
    private StatsPage _statsPage;
    private LightingManager _lightingManager;
    
    // Virtual resolution system
    private VirtualResolution _virtualResolution;
    
    // Render targets for lighting
    private RenderTarget2D _sceneRenderTarget;
    
    // Zone transition effects
    private bool _isTransitioning;
    
    // PoI tracking
    private HashSet<Guid> _approachedPoIs;
    private float _transitionTimer;
    private float _transitionDuration = 0.5f; // 0.5 seconds
    private Texture2D _fadeTexture;
    
    // Input handling
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // Initial window size - will be managed by VirtualResolution
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
    }

    protected override void Initialize()
    {
        try
        {
            System.Console.WriteLine("Initializing game systems...");
            
            // Initialize virtual resolution system
            _virtualResolution = new VirtualResolution(_graphics, GraphicsDevice, this, 1024, 768);
            System.Console.WriteLine("VirtualResolution created");
            
            // Initialize PoI tracking
            _approachedPoIs = new HashSet<Guid>();
            
            // Initialize game systems
            _assetManager = new AssetManager(Content);
            System.Console.WriteLine("AssetManager created");
            
            _audioManager = new AudioManager(Content);
            System.Console.WriteLine("AudioManager created");
            
            _adventurer = new Adventurer(new Vector2(1000, 1000)); // Start in center of first zone
            System.Console.WriteLine("Adventurer created");
            
            _camera = new Camera(_virtualResolution.GetVirtualViewport());
            System.Console.WriteLine("Camera created");
            
            _zoneManager = new ZoneManager(12345); // Fixed seed for consistent zones
            System.Console.WriteLine("ZoneManager created");
            
            // Initialize time system
            _timeManager = new TimeManager();
            _timeManager.SetDayNightCycle(1f, .5f); // 1 min day, 30 sec night - easily changeable!
            System.Console.WriteLine("TimeManager created");
            
            // Initialize weather system
            _weatherManager = new WeatherManager(_timeManager, 12345);
            System.Console.WriteLine("WeatherManager created");
            
            // Initialize weather effects
            _weatherEffects = new WeatherEffects(GraphicsDevice, _camera);
            _weatherEffects.UpdateScreenBounds(new Rectangle(0, 0, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight));
            System.Console.WriteLine("WeatherEffects created");
            
            // Initialize journal system
            _journalManager = new JournalManager(_timeManager);
            _journalUI = new JournalUI(GraphicsDevice, _journalManager);
            
            // Initialize PoI system
            _poiManager = new PoIManager(_assetManager, _journalManager, 12345);
            
            // Subscribe to PoI events for journal entries
            _poiManager.PoIDiscovered += (poi) => {
                _journalManager.OnSpecialEvent($"Discovered {poi.Name}", poi.Description);
            };
            
            _poiManager.PoIInteracted += (poi, adventurer) => {
                _journalManager.OnSpecialEvent($"Interacted with {poi.Name}", $"Met with {poi.Name} - {poi.Description}");
            };
            
            _poiManager.PoIApproached += (poi) => {
                // Only add journal entry on first approach to avoid spam
                if (!_approachedPoIs.Contains(poi.Id))
                {
                    _approachedPoIs.Add(poi.Id);
                    _journalManager.OnSpecialEvent($"Approached {poi.Name}", $"Came across {poi.Name} - {poi.Description}");
                }
            };
            
            // Initialize quest system
            _questManager = new QuestManager(_journalManager, _weatherManager, _poiManager, _timeManager);
            
            // Initialize stats system
            _statsManager = new StatsManager();
            _statsManager.Initialize(_timeManager, _weatherManager, _questManager, _poiManager, _journalManager);
            _adventurer.SetStatsManager(_statsManager);
            _adventurer.SetAudioManager(_audioManager);
            System.Console.WriteLine("StatsManager created and initialized");
            
            // Initialize lighting system
            _lightingManager = new LightingManager(GraphicsDevice, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
            _adventurer.SetLightingManager(_lightingManager);
            System.Console.WriteLine("LightingManager created and connected to adventurer");
            
            // Subscribe to weather changes for journal tracking
            _weatherManager.WeatherChanged += (weather) => {
                _journalManager.OnWeatherChanged(weather, _timeManager.GetSeasonName());
            };
            
            System.Console.WriteLine("Journal and PoI systems created");
            
            // Initialize UI system (will be created after zone manager loads)
            System.Console.WriteLine("UIManager will be created after zone loading");
            
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();

            base.Initialize();
            System.Console.WriteLine("Game initialization complete");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error during initialization: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    protected override void LoadContent()
    {
        try
        {
            System.Console.WriteLine("Loading content...");
            
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            System.Console.WriteLine("SpriteBatch created");
            
            // Create render target for scene (for multiply lighting) - use virtual resolution
            _sceneRenderTarget = new RenderTarget2D(
                GraphicsDevice,
                _virtualResolution.VirtualWidth,
                _virtualResolution.VirtualHeight
            );
            System.Console.WriteLine("Scene render target created");
            
            // Load all assets
            _assetManager.LoadAssets();
            System.Console.WriteLine("Assets loaded");
            
            // Load audio assets
            _audioManager.LoadSounds();
            System.Console.WriteLine("Audio loaded");
            
            // Load journal entry data
            JournalEntryData.Instance.LoadContent(Content);
            System.Console.WriteLine("Journal entry data loaded");
            
            // Set up adventurer sprites
            _adventurer.LoadContent(
                _assetManager.GetTexture("adventurer"), 
                _assetManager.GetTexture("adventurer_walking")
            );
            // Optional: sleeping animation if available
            var sleepingTex = _assetManager.GetTexture("adventurer_sleeping");
            if (sleepingTex != null)
            {
                _adventurer.LoadSleepingTexture(sleepingTex);
            }
            // Torch sprite for night
            var torchTex = _assetManager.GetTexture("adventurer_torch");
            if (torchTex != null)
            {
                _adventurer.LoadTorchTexture(torchTex);
            }
            
            // Load sleeping texture separately if available
            var sleepingTexture = _assetManager.GetTexture("adventurer_sleeping");
            if (sleepingTexture != null)
            {
                _adventurer.LoadSleepingTexture(sleepingTexture);
            }
            System.Console.WriteLine("Adventurer content loaded");
            
            // Set up zone manager
            _zoneManager.LoadContent(_assetManager, _poiManager);
            System.Console.WriteLine("ZoneManager content loaded");
            
            // Set up PoI manager
            _poiManager.LoadContent();
            System.Console.WriteLine("PoI content loaded");
            
            // Initialize PoIManager with the starting zone
            _poiManager.SetCurrentZone(_zoneManager.CurrentZone.Id);
            System.Console.WriteLine($"PoIManager initialized with starting zone: {_zoneManager.CurrentZone.Id}");
            
            // Now create UI manager with zone manager reference
            _uiManager = new UIManager(GraphicsDevice, _timeManager, _zoneManager, _weatherManager, _statsManager, _journalManager);
            _uiManager.UpdateScreenSize(_virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
            System.Console.WriteLine("UIManager created");
            
            // Create stats page
            _statsPage = new StatsPage(GraphicsDevice, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
            System.Console.WriteLine("StatsPage created");
            
            // Create minimap in top-right corner
            Rectangle minimapArea = new Rectangle(
                _virtualResolution.VirtualWidth - 220, 10, 
                200, 200
            );
            _miniMap = new MiniMap(GraphicsDevice, minimapArea, _zoneManager);
            System.Console.WriteLine("MiniMap created");
            
            // Create fade texture for transitions
            _fadeTexture = new Texture2D(GraphicsDevice, 1, 1);
            _fadeTexture.SetData(new[] { Color.Black });
            System.Console.WriteLine("Fade texture created");
            
            // Load UI font
            try
            {
                var font = Content.Load<SpriteFont>("fonts/Arial");
                _uiManager.LoadContent(font);
                _uiManager.LoadUITextures(Content);
                _journalUI.LoadContent(font);
                _statsPage.LoadContent(font);
                System.Console.WriteLine("UI font and textures loaded successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load font: {ex.Message}");
                System.Console.WriteLine("UI will use fallback graphics instead of text");
                // Continue without font - UI will handle null font gracefully
            }
            
            System.Console.WriteLine("Content loading complete");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error during LoadContent: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        try
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Handle keyboard input for zoom control
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add))
            {
                _camera.Zoom = MathHelper.Clamp(_camera.Zoom + 0.02f, 0.5f, 4f);
            }
            if (keyboardState.IsKeyDown(Keys.OemMinus) || keyboardState.IsKeyDown(Keys.Subtract))
            {
                _camera.Zoom = MathHelper.Clamp(_camera.Zoom - 0.02f, 0.5f, 4f);
            }

            // Handle mouse input for UI and interaction
            var currentMouseState = Mouse.GetState();
            if (currentMouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
                // Convert screen coordinates to virtual coordinates
                Vector2 virtualMousePos = _virtualResolution.ScreenToVirtual(mousePos);
                
                // Check UI clicks first (pause button, etc.)
                if (!_uiManager.HandleMouseClick(virtualMousePos))
                {
                    // If UI didn't handle it, convert to world coordinates for game interaction
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(virtualMousePos);
                    // You can add logic here to influence the adventurer's direction toward the click
                }
            }
            
            // Handle mouse hover for tooltips
            Vector2 currentMousePos = _virtualResolution.ScreenToVirtual(new Vector2(currentMouseState.X, currentMouseState.Y));
            _uiManager.HandleMouseHover(currentMousePos, out string tooltip);
            
            _previousMouseState = currentMouseState;

            // Handle spacebar for pause toggle
            if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
            {
                _uiManager.SetPaused(!_uiManager.IsPaused);
            }
            
            // Handle P key for pathfinding status
            if (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P))
            {
                System.Console.WriteLine("[DEBUG] P key pressed!");
                System.Console.WriteLine($"[PATHFINDING TEST] Current Status: {_adventurer.GetPathfindingStatus()}");
                System.Console.WriteLine($"[PATHFINDING TEST] Position: {_adventurer.Position}");
                System.Console.WriteLine($"[PATHFINDING TEST] Current Zone: {_zoneManager.CurrentZone.Name} ({_zoneManager.CurrentZone.BiomeType})");
                
                // Print nearby PoIs for testing
                var nearbyPoIs = _poiManager.GetNearbyPoIs(_adventurer.Position, 200f, _zoneManager.CurrentZone?.Id);
                System.Console.WriteLine($"[PATHFINDING TEST] Nearby PoIs ({nearbyPoIs.Count}):");
                foreach (var poi in nearbyPoIs.Take(5)) // Show first 5
                {
                    float distance = Vector2.Distance(_adventurer.Position, poi.Position);
                    System.Console.WriteLine($"  - {poi.Type} at distance {distance:F1}");
                }
                
                // Print quest status
                _questManager.PrintQuestStatus();
            }
            
            // Handle S key for stats page toggle
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
            {
                _statsPage?.Toggle();
            }
            
            // Handle R key for aspect ratio toggle
            if (keyboardState.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
            {
                var newMode = _virtualResolution.CurrentMode == AspectRatioMode.Regular 
                    ? AspectRatioMode.TikTok 
                    : AspectRatioMode.Regular;
                    
                _virtualResolution.SetAspectRatioMode(newMode);
                
                // Update systems that depend on screen size
                _weatherEffects.UpdateScreenBounds(new Rectangle(0, 0, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight));
                _uiManager.UpdateScreenSize(_virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
                _camera.UpdateViewport(_virtualResolution.GetVirtualViewport());
                
                // Update minimap position for new aspect ratio
                Rectangle newMinimapArea = new Rectangle(
                    _virtualResolution.VirtualWidth - 220, 10, 
                    200, 200
                );
                _miniMap?.UpdateArea(newMinimapArea);
                
                // Recreate render targets with new size
                _sceneRenderTarget?.Dispose();
                _sceneRenderTarget = new RenderTarget2D(
                    GraphicsDevice,
                    _virtualResolution.VirtualWidth,
                    _virtualResolution.VirtualHeight
                );
                
                // Update lighting manager
                _lightingManager?.Dispose();
                _lightingManager = new LightingManager(GraphicsDevice, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
                _adventurer.SetLightingManager(_lightingManager);
                
                System.Console.WriteLine($"Switched to {newMode} mode ({_virtualResolution.ScreenWidth}x{_virtualResolution.ScreenHeight})");
            }
            
            _previousKeyboardState = keyboardState;

            // Update transition effects
            if (_isTransitioning)
            {
                _transitionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                if (_transitionTimer >= _transitionDuration)
                {
                    _isTransitioning = false;
                    _transitionTimer = 0f;
                }
                
                // Don't update game systems during transition
                return;
            }
            
            // Update game systems
            if (_adventurer != null && _zoneManager != null)
            {
                // Always update journal UI and stats page (can be opened while paused)
                _journalUI.Update(gameTime);
                _statsPage.Update(gameTime);
                
                // Only update game simulation if not paused
                if (!_uiManager.IsPaused)
                {
                    // Update time system
                    _timeManager.Update(gameTime);
                    // Toggle adventurer night mode for torch sprite
                    _adventurer.SetNightMode(_timeManager.CurrentTimeOfDay == TimeOfDay.Night);
                    
                    // Update weather system
                    _weatherManager.Update(gameTime);
                    
                    // Update weather effects
                    _weatherEffects.Update(gameTime, _weatherManager.CurrentWeather, _weatherManager.WeatherIntensity);
                    
                    // Update PoI system
                    _poiManager.Update(_adventurer.Position, 32f, _zoneManager.CurrentZone?.Id);
                    
                    // Update stats system
                    _statsManager.Update(gameTime);
                    
                    // Update audio system
                    _audioManager?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                    
                    // Update lighting system
                    if (_lightingManager != null)
                    {
                        // Set ambient light based on time of day
                        if (_timeManager.CurrentTimeOfDay == TimeOfDay.Night)
                        {
                            // Dark ambient for multiply lighting (lightmap background color)
                            // This is what unlit areas will be multiplied by
                            _lightingManager.AmbientColor = new Color(15, 20, 40); // Dark blue for night
                        }
                        else
                        {
                            _lightingManager.AmbientColor = Color.White; // Full brightness during day
                        }
                        
                        _lightingManager.Update(gameTime);
                    }
                    else
                    {
                        System.Console.WriteLine("[GAME1] WARNING: LightingManager is null in Update!");
                    }
                    
                    // Update UI manager (for ticker animations)
                    _uiManager.Update(gameTime);
                    
                    // Update adventurer (this may trigger zone changes)
                    _adventurer.Update(gameTime, _zoneManager, _poiManager, _questManager);
                    bool zoneChanged = _zoneManager.ZoneChanged;
                    
                    _camera.Follow(_adventurer.Position);
                    _camera.Update(gameTime);
                    _zoneManager.Update(_adventurer.Position);
                    
                    // Handle zone changes after all updates
                    if (zoneChanged && _miniMap != null)
                    {
                        // Start transition effect
                        _isTransitioning = true;
                        _transitionTimer = 0f;
                        
                        // Snap camera to new position immediately
                        _camera.SnapToPosition(_adventurer.Position);
                        
                        _miniMap.OnZoneChanged();
                        
                        // Update PoIManager with new zone
                        _poiManager.SetCurrentZone(_zoneManager.CurrentZone.Id);
                        
                        // Record zone visit in journal
                        _journalManager.OnZoneEntered(_zoneManager.CurrentZone);
                        
                        // Check quest objectives for zone exploration
                        _questManager.OnZoneEntered(_zoneManager.CurrentZone);
                        
                        // Update stats for zone exploration
                        _statsManager.OnZoneEntered(_zoneManager.CurrentZone);
                        
                        System.Console.WriteLine($"Zone transition started to: {_zoneManager.CurrentZone.Name}");
                    }
                }
            }

            base.Update(gameTime);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error during Update: {ex.Message}");
            // Don't rethrow in Update to prevent crash loops
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        try
        {
            if (_spriteBatch != null && _zoneManager != null && _adventurer != null)
            {
                // Begin drawing to virtual resolution
                _virtualResolution.BeginVirtualDraw();
                
                // STEP 1: Render scene to texture (at night) or directly to virtual target (during day)
                if (_timeManager.CurrentTimeOfDay == TimeOfDay.Night)
                {
                    // Render scene to texture for multiply lighting
                    GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
                    GraphicsDevice.Clear(Color.Black); // Clear to black
                }
                
                // Draw world with camera transform
                _spriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix(), samplerState: SamplerState.PointClamp);
                
                // Draw zone (terrain and objects)
                _zoneManager.Draw(_spriteBatch);
                
                // Draw PoIs (buildings, NPCs, etc.)
                _poiManager.Draw(_spriteBatch, _zoneManager.CurrentZone?.Id);
                
                // Draw adventurer (foreground)
                _adventurer.Draw(_spriteBatch);
                
                _spriteBatch.End();
                
                // STEP 2: At night, render lightmap and multiply with scene
                if (_timeManager.CurrentTimeOfDay == TimeOfDay.Night && _lightingManager != null)
                {
                    // Render lightmap (black background + white/orange lights)
                    _lightingManager.BeginLightMap(_camera);
                    _lightingManager.DrawLights(_spriteBatch, _camera);
                    _lightingManager.EndLightMap();
                    
                    // Switch back to virtual render target (we're already in virtual draw mode)
                    _virtualResolution.BeginVirtualDraw();
                    
                    // STEP 3: Multiply scene × lightmap
                    var multiplyBlend = new BlendState
                    {
                        ColorBlendFunction = BlendFunction.Add,
                        ColorSourceBlend = Blend.DestinationColor,  // Scene color
                        ColorDestinationBlend = Blend.Zero,
                        AlphaBlendFunction = BlendFunction.Add,
                        AlphaSourceBlend = Blend.One,
                        AlphaDestinationBlend = Blend.Zero
                    };
                    
                    // Draw scene to virtual target
                    _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
                    _spriteBatch.Draw(_sceneRenderTarget, Vector2.Zero, Color.White);
                    _spriteBatch.End();
                    
                    // Multiply with lightmap
                    _spriteBatch.Begin(SpriteSortMode.Immediate, multiplyBlend, SamplerState.LinearClamp);
                    _spriteBatch.Draw(
                        _lightingManager.GetLightMap(),
                        new Rectangle(0, 0, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight),
                        Color.White
                    );
                    _spriteBatch.End();
                }

                // Draw UI elements without camera transform
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                
                // Draw minimap
                if (_miniMap != null)
                {
                    _miniMap.Draw(_spriteBatch, _adventurer.Position);
                }
                
                // Draw UI (clock, day counter)
                _uiManager.Draw(_spriteBatch);
                
                // Draw weather effects (rain, snow) on top of UI
                _weatherEffects.Draw(_spriteBatch);
                
                // Draw journal if open
                _journalUI.Draw(_spriteBatch);
                
                // Draw stats page if open
                if (_statsPage != null && _statsManager != null)
                {
                    _statsPage.Draw(_spriteBatch, _statsManager.CurrentStats);
                }
                
                _spriteBatch.End();
                
                // Apply weather effects
                Color weatherTint = _weatherManager.GetWeatherTint();
                if (weatherTint != Color.Transparent)
                {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
                    Rectangle virtualRect = new Rectangle(0, 0, 
                        _virtualResolution.VirtualWidth, 
                        _virtualResolution.VirtualHeight);
                    
                    _spriteBatch.Draw(_fadeTexture, virtualRect, weatherTint);
                    _spriteBatch.End();
                }
                
                // Draw transition fade effect
                if (_isTransitioning)
                {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    
                    // Calculate fade alpha based on transition progress
                    float progress = _transitionTimer / _transitionDuration;
                    float alpha;
                    
                    if (progress < 0.5f)
                    {
                        // Fade to black (first half)
                        alpha = progress * 2f;
                    }
                    else
                    {
                        // Fade from black (second half)
                        alpha = (1f - progress) * 2f;
                    }
                    
                    Rectangle virtualRect = new Rectangle(0, 0, 
                        _virtualResolution.VirtualWidth, 
                        _virtualResolution.VirtualHeight);
                    
                    _spriteBatch.Draw(_fadeTexture, virtualRect, Color.Black * alpha);
                    _spriteBatch.End();
                }
                
                // End virtual drawing
                _virtualResolution.EndVirtualDraw();
                
                // Draw virtual resolution to actual screen
                _virtualResolution.DrawVirtualToScreen(_spriteBatch);
            }

            base.Draw(gameTime);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error during Draw: {ex.Message}");
            // Don't rethrow in Draw to prevent crash loops
        }
    }

    protected override void UnloadContent()
    {
        _assetManager?.UnloadAssets();
        _audioManager?.Dispose();
        _miniMap?.Dispose();
        _uiManager?.Dispose();
        _weatherEffects?.Dispose();
        _journalUI?.Dispose();
        _statsPage?.Dispose();
        _statsManager?.Dispose();
        _lightingManager?.Dispose();
        _virtualResolution?.Dispose();
        _sceneRenderTarget?.Dispose();
        _fadeTexture?.Dispose();
        base.UnloadContent();
    }
}