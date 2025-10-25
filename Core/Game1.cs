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
    private BackgroundMusicManager _backgroundMusicManager;
    private Adventurer _adventurer;
    private Camera _camera;
    private ZoneManager _zoneManager;
    private MiniMap _miniMap;
    private TimeManager _timeManager;
    private TimeOfDay _lastTimeOfDay;
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
    private EventCardBox _eventCardBox;
    private DialogueManager _dialogueManager;
    
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
            
            // Add some test items to inventory
            _adventurer.CollectItem("berries", 5);
            _adventurer.CollectItem("wood", 10);
            _adventurer.CollectItem("shiny_stone", 1);
            _adventurer.CollectItem("flower", 3);
            System.Console.WriteLine("Test items added to inventory");
            
            _camera = new Camera(_virtualResolution.GetVirtualViewport());
            System.Console.WriteLine("Camera created");
            
            _zoneManager = new ZoneManager(12345); // Fixed seed for consistent zones
            System.Console.WriteLine("ZoneManager created");
            
            // Initialize time system
            _timeManager = new TimeManager();
            // Set day length - 24 game hours = 2 real minutes (120 seconds)
            _timeManager.SetDayLength(120f); // Simple clock that runs at constant speed
            _lastTimeOfDay = _timeManager.CurrentTimeOfDay;
            System.Console.WriteLine("TimeManager created");
            
            // Background music manager will be created after weather manager
            
            // Initialize weather system
            _weatherManager = new WeatherManager(_timeManager, 12345);
            System.Console.WriteLine("WeatherManager created");
            
            // Update background music manager with weather manager
            _backgroundMusicManager = new BackgroundMusicManager(Content, _audioManager, _timeManager, _weatherManager);
            System.Console.WriteLine("BackgroundMusicManager updated with WeatherManager");
            
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
            
            // PoIInteracted event will be subscribed after DialogueManager initialization
            
            _poiManager.PoIApproached += (poi) => {
                // Only add journal entry on first approach to avoid spam
                if (!_approachedPoIs.Contains(poi.Id))
                {
                    _approachedPoIs.Add(poi.Id);
                    _journalManager.OnSpecialEvent($"Approached {poi.Name}", $"Came across {poi.Name} - {poi.Description}");
                }
            };
            
            // Initialize quest system
            _questManager = new QuestManager(_journalManager, _weatherManager, _poiManager, _timeManager, _statsManager);
            
            // Initialize stats system
            _statsManager = new StatsManager();
            _statsManager.Initialize(_timeManager, _weatherManager, _questManager, _poiManager, _journalManager);
            _adventurer.SetStatsManager(_statsManager);
            _adventurer.SetAudioManager(_audioManager);
            System.Console.WriteLine("StatsManager created and initialized");
            
            // Initialize lighting system
            _lightingManager = new LightingManager(GraphicsDevice, _virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
            _adventurer.SetLightingManager(_lightingManager);
            _poiManager.SetLightingManager(_lightingManager);
            System.Console.WriteLine("LightingManager created and connected to adventurer and PoI system");
            
            // Initialize EventCardBox and DialogueManager
            _eventCardBox = new EventCardBox(GraphicsDevice);
            _eventCardBox.UpdateScreenSize(_virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
            
            _dialogueManager = new DialogueManager(_questManager, _statsManager, _journalManager, _assetManager, _eventCardBox);
            System.Console.WriteLine("EventCardBox and DialogueManager created");
            
            // Subscribe to dialogue events for game state management
            _dialogueManager.ConversationStarted += () => {
                // Pause game systems when dialogue starts
                _uiManager?.SetPaused(true);
                System.Console.WriteLine("[Game1] Paused game for dialogue");
            };
            
            _dialogueManager.ConversationEnded += () => {
                // Resume game systems when dialogue ends
                _uiManager?.SetPaused(false);
                
                // Restore background music volume after interaction
                _backgroundMusicManager?.SetInInteraction(false);
                
                System.Console.WriteLine("[Game1] Resumed game after dialogue ended");
            };
            
            // Subscribe to PoI interactions for dialogue triggering
            // This replaces the existing PoIInteracted subscription to add dialogue support
            _poiManager.PoIInteracted += OnPoIInteracted;
            
            // Subscribe to weather changes for journal tracking and audio
            _weatherManager.WeatherChanged += (weather) => {
                _journalManager.OnWeatherChanged(weather, _timeManager.GetSeasonName());
                
                // Handle weather sounds
                if (_audioManager != null)
                {
                    if (weather == WeatherType.Rain)
                    {
                        _audioManager.StartRainSound(_weatherManager.WeatherIntensity);
                    }
                    else
                    {
                        // Stop rain sound when weather changes to anything else
                        _audioManager.StopRainSound();
                    }
                }
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
            
            // Load background music
            _backgroundMusicManager.LoadMusic();
            System.Console.WriteLine("Background music loaded");
            
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
            
            // Set up PoI manager FIRST (must load JSON data before zone generation)
            _poiManager.LoadContent();
            System.Console.WriteLine("PoI content loaded");
            
            // Set up zone manager (this will call GeneratePoIsForZone)
            _zoneManager.LoadContent(_assetManager, _poiManager);
            System.Console.WriteLine("ZoneManager content loaded");
            
            // Initialize PoIManager with the starting zone
            _poiManager.SetCurrentZone(_zoneManager.CurrentZone.Id);
            System.Console.WriteLine($"PoIManager initialized with starting zone: {_zoneManager.CurrentZone.Id}");
            
            // DEBUG: Uncomment to spawn all PoI types in first zone for testing
            //  _poiManager.DEBUG_SpawnAllPoITypes(_zoneManager.CurrentZone);
            //  System.Console.WriteLine("[DEBUG] All PoI types spawned in starting zone");
            
            // Now create UI manager with zone manager reference
            _uiManager = new UIManager(GraphicsDevice, _timeManager, _zoneManager, _weatherManager, _statsManager, _journalManager);
            _uiManager.UpdateScreenSize(_virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
            _uiManager.SetAudioManager(_audioManager);
            _uiManager.SetInventoryManager(_adventurer.GetInventoryManager());
            
            // Wire up escape menu exit event
            if (_uiManager.EscapeMenu != null)
            {
                _uiManager.EscapeMenu.OnExitRequested += () => Exit();
            }
            
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
                var font = Content.Load<SpriteFont>("fonts/04b03");
                _uiManager.LoadContent(font);
                _uiManager.LoadUITextures(Content);
                _journalUI.LoadContent(font);
                _statsPage.LoadContent(font);
                
                // Load EventCardBox font
                _eventCardBox.Font = font;
                
                // Load DialogueManager data
                _dialogueManager.LoadDialogueData();
                
                System.Console.WriteLine("UI font and textures loaded successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load font: {ex.Message}");
                System.Console.WriteLine("UI will use fallback graphics instead of text");
                // Continue without font - UI will handle null font gracefully
            }
            
            System.Console.WriteLine("Content loading complete");
            
            // Start background music
            _backgroundMusicManager?.StartMusic();
            System.Console.WriteLine("Background music started");
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
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
                
                // Check EventCardBox clicks first (highest priority)
                if (_eventCardBox.IsVisible && _eventCardBox.HandleMouseClick(virtualMousePos))
                {
                    // EventCardBox handled the click
                }
                // Check UI clicks (escape menu, pause button, etc.)
                else if (!_uiManager.HandleMouseClick(virtualMousePos))
                {
                    // If UI didn't handle it and escape menu is not open, convert to world coordinates for game interaction
                    if (!_uiManager.IsEscapeMenuVisible)
                    {
                        Vector2 mouseWorldPos = _camera.ScreenToWorld(virtualMousePos);
                        // You can add logic here to influence the adventurer's direction toward the click
                    }
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
            
            // Handle escape key for escape menu
            if (keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                _uiManager.ToggleEscapeMenu();
            }
            
            // Handle P key for pathfinding status
            if (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P))
            {
                System.Console.WriteLine("[DEBUG] P key pressed!");
                System.Console.WriteLine($"[PATHFINDING TEST] Current Status: {_adventurer.GetPathfindingStatus()}");
                System.Console.WriteLine($"[PATHFINDING TEST] Position: {_adventurer.Position}");
                System.Console.WriteLine($"[PATHFINDING TEST] Current Zone: {_zoneManager.CurrentZone.Name} ({_zoneManager.CurrentZone.BiomeType})");
                
                // Print total PoI count for debugging
                int totalPoIs = _poiManager.GetPoICountForZone(_zoneManager.CurrentZone.Id);
                System.Console.WriteLine($"[DEBUG] Total PoIs in current zone: {totalPoIs}");
                
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
            
            // Handle L key for light diagnostic
            if (keyboardState.IsKeyDown(Keys.L) && !_previousKeyboardState.IsKeyDown(Keys.L))
            {
                System.Console.WriteLine("[DEBUG] L key pressed - Running light diagnostic...");
                _poiManager.DEBUG_DiagnosePhantomLights();
            }
            
            // Handle S key for stats page toggle
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
            {
                _statsPage?.Toggle();
            }
            
            // Handle I key for inventory toggle
            if (keyboardState.IsKeyDown(Keys.I) && !_previousKeyboardState.IsKeyDown(Keys.I))
            {
                _uiManager?.ToggleInventory();
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
                _eventCardBox.UpdateScreenSize(_virtualResolution.VirtualWidth, _virtualResolution.VirtualHeight);
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
                // Always update journal UI, stats page, UI manager, and EventCardBox (can be opened while paused)
                _journalUI.Update(gameTime);
                _statsPage.Update(gameTime);
                _eventCardBox.Update(gameTime);
                _uiManager.Update(gameTime); // Moved here so escape menu can update
                
                // Update background music system (always update so volume changes work while paused)
                _backgroundMusicManager?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                
                // Allow stats system to update during dialogue for reward processing
                if (_dialogueManager.IsConversationActive())
                {
                    _statsManager.Update(gameTime);
                }
                
                // Only update game simulation if not paused and escape menu is not open
                if (!_uiManager.IsPaused && !_uiManager.IsEscapeMenuVisible)
                {
                    // Update time system
                    _timeManager.Update(gameTime);
                    // Toggle adventurer night mode for torch sprite and light visibility
                    // Keep torch on during dark periods: Night, Dawn, and Dusk
                    bool needsLight = _timeManager.CurrentTimeOfDay != TimeOfDay.Day;
                    _adventurer.SetNightMode(needsLight);
                    
                    // Control PoI lights based on time of day
                    _poiManager.SetLightsEnabled(needsLight);
                    
                    // Update weather system
                    _weatherManager.Update(gameTime);
                    
                    // Update weather effects
                    _weatherEffects.Update(gameTime, _weatherManager.CurrentWeather, _weatherManager.WeatherIntensity);
                    
                    // Update PoI system
                    _poiManager.Update(_adventurer.Position, 32f, _zoneManager.CurrentZone?.Id);
                    
                    // Update stats system
                    _statsManager.Update(gameTime);
                    
                    // Update quest system
                    _questManager?.Update();
                    
                    // Update audio system
                    _audioManager?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                    
                    // Update lighting system
                    if (_lightingManager != null)
                    {
                        // Set ambient light based on time of day AND weather
                        var timeAmbient = _timeManager.GetAmbientColor();
                        var weatherAmbient = GetCombinedWeatherAmbient(timeAmbient);
                        _lightingManager.AmbientColor = weatherAmbient;
                        
                        // Debug output for lighting changes (only when time period changes)
                        if (_lastTimeOfDay != _timeManager.CurrentTimeOfDay)
                        {
                            System.Console.WriteLine($"[LIGHTING] Time changed to {_timeManager.CurrentTimeOfDay} - Combined Ambient: R{weatherAmbient.R} G{weatherAmbient.G} B{weatherAmbient.B} - {_timeManager.GetTimeString()} - Weather: {_weatherManager.CurrentWeather}");
                            _lastTimeOfDay = _timeManager.CurrentTimeOfDay;
                        }
                        
                        _lightingManager.Update(gameTime);
                    }
                    else
                    {
                        System.Console.WriteLine("[GAME1] WARNING: LightingManager is null in Update!");
                    }
                    
                    // Update adventurer (this may trigger zone changes)
                    Vector2 previousPosition = _adventurer.Position;
                    _adventurer.Update(gameTime, _zoneManager, _poiManager, _questManager);
                    bool zoneChanged = _zoneManager.ZoneChanged;
                    
                    // Update background music with movement state
                    bool isMoving = Vector2.Distance(previousPosition, _adventurer.Position) > 0.1f;
                    _backgroundMusicManager?.SetPlayerMoving(isMoving);
                    
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
                        _questManager.OnBiomeVisited(_zoneManager.CurrentZone.BiomeType);
                        
                        // Update stats for zone exploration
                        _statsManager.OnZoneEntered(_zoneManager.CurrentZone);
                        

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
                
                // STEP 1: Always render scene to texture for lighting system
                // Render scene to texture for multiply lighting
                GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
                GraphicsDevice.Clear(Color.Black); // Clear to black
                
                // Draw world with camera transform
                _spriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix(), samplerState: SamplerState.PointClamp);
                
                // Draw terrain (ground tiles) - bottom layer
                _zoneManager.DrawTerrain(_spriteBatch);
                
                // Draw adventurer (middle layer - above ground, below trees)
                _adventurer.Draw(_spriteBatch);
                
                // Draw PoIs (buildings, NPCs, etc.) - same layer as adventurer
                _poiManager.Draw(_spriteBatch, _zoneManager.CurrentZone?.Id);
                
                // Draw objects (trees, rocks, etc.) - top layer
                _zoneManager.DrawObjects(_spriteBatch);
                
                _spriteBatch.End();
                
                // STEP 2: Always render lightmap and multiply with scene for all time periods
                if (_lightingManager != null)
                {
                    // Render lightmap with ambient color based on time of day
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
                
                // Draw EventCardBox if visible (should be on top of other UI)
                _eventCardBox.Draw(_spriteBatch);
                
                _spriteBatch.End();
                
                // Weather effects are now handled through ambient lighting system
                
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
    
    /// <summary>
    /// Handles PoI interactions and triggers dialogue if available
    /// </summary>
    private void OnPoIInteracted(PointOfInterest poi, Adventurer adventurer)
    {
        try
        {
            // Always add journal entry for the interaction
            _journalManager.OnSpecialEvent($"Interacted with {poi.Name}", $"Met with {poi.Name} - {poi.Description}");
            
            // Check if this PoI has associated dialogue/event data
            if (_dialogueManager != null && HasDialogueForPoI(poi.Type))
            {
                // Pause game systems while dialogue is active
                _uiManager?.SetPaused(true);
                
                // Lower background music during interaction
                _backgroundMusicManager?.SetInInteraction(true);
                
                // Trigger the EventCardBox with the dialogue
                _dialogueManager.TriggerDialogue(poi.Type, GetRelatedQuest(poi));
                
                System.Console.WriteLine($"[Game1] Triggered dialogue for PoI: {poi.Type}");
            }
            else
            {
                // No dialogue exists, continue with existing PoI interaction behavior
                System.Console.WriteLine($"[Game1] No dialogue available for PoI: {poi.Type}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Game1] Error handling PoI interaction: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Combines time-of-day ambient lighting with weather effects
    /// </summary>
    private Color GetCombinedWeatherAmbient(Color timeAmbient)
    {
        if (_weatherManager == null)
            return timeAmbient;

        // Get weather influence from WeatherManager
        var weatherInfluence = _weatherManager.GetWeatherLightingInfluence();

        // Multiply time ambient with weather influence
        return new Color(
            (timeAmbient.R / 255f) * (weatherInfluence.R / 255f),
            (timeAmbient.G / 255f) * (weatherInfluence.G / 255f),
            (timeAmbient.B / 255f) * (weatherInfluence.B / 255f),
            timeAmbient.A / 255f
        );
    }

    /// <summary>
    /// Checks if a PoI type has associated dialogue data
    /// </summary>
    private bool HasDialogueForPoI(PoIType poiType)
    {
        // Check if DialogueManager actually has dialogue data for this PoI type
        if (_dialogueManager == null)
            return false;
            
        var availableDialogues = _dialogueManager.GetDialogueTrees();
        
        // Check if any dialogue tree is associated with this PoI type
        foreach (var tree in availableDialogues.Values)
        {
            if (tree.AssociatedNPC == poiType)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets a related quest for the PoI if one exists
    /// </summary>
    private Quest GetRelatedQuest(PointOfInterest poi)
    {
        try
        {
            // Check if any active quests are related to this PoI
            var activeQuests = _questManager?.GetActiveQuests() ?? new List<Quest>();
            
            foreach (var quest in activeQuests)
            {
                // Check if quest involves this PoI type or location
                if (poi.AssociatedQuests.Contains(quest.Id.ToString()) ||
                    quest.Name.Contains(poi.Type.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return quest;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Game1] Error getting related quest: {ex.Message}");
            return null;
        }
    }

    protected override void UnloadContent()
    {
        _assetManager?.UnloadAssets();
        _audioManager?.Dispose();
        _backgroundMusicManager?.Dispose();
        _miniMap?.Dispose();
        _uiManager?.Dispose();
        _weatherEffects?.Dispose();
        _journalUI?.Dispose();
        _statsPage?.Dispose();
        _eventCardBox?.Dispose();
        _statsManager?.Dispose();
        _lightingManager?.Dispose();
        _virtualResolution?.Dispose();
        _sceneRenderTarget?.Dispose();
        _fadeTexture?.Dispose();
        base.UnloadContent();
    }
}