using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Threading.Tasks;

namespace HiddenHorizons;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    // Game systems
    private AssetManager _assetManager;
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
    private UIManager _uiManager;
    private SaveManager _saveManager;
    private SaveSlotManager _saveSlotManager;
    private EscapeMenuUI _escapeMenuUI;
    private SaveLoadUI _saveLoadUI;
    
    // Zone transition effects
    private bool _isTransitioning;
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
        
        // Set window size for comfortable viewing
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
    }

    protected override void Initialize()
    {
        try
        {
            System.Console.WriteLine("Initializing game systems...");
            
            // Initialize game systems
            _assetManager = new AssetManager(Content);
            System.Console.WriteLine("AssetManager created");
            
            _adventurer = new Adventurer(new Vector2(1000, 1000)); // Start in center of first zone
            System.Console.WriteLine("Adventurer created");
            
            _camera = new Camera(GraphicsDevice.Viewport);
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
            _weatherEffects.UpdateScreenBounds(new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight));
            System.Console.WriteLine("WeatherEffects created");
            
            // Initialize journal system
            _journalManager = new JournalManager(_timeManager);
            _journalUI = new JournalUI(GraphicsDevice, _journalManager);
            
            // Initialize PoI system
            _poiManager = new PoIManager(_assetManager, _journalManager, 12345);
            
            // Subscribe to weather changes for journal tracking and auto-save
            _weatherManager.WeatherChanged += (weather) => {
                _journalManager.OnWeatherChanged(weather, _timeManager.GetSeasonName());
                // Trigger auto-save on significant weather changes
                _saveManager?.TriggerAutoSave(AutoSaveTrigger.WeatherChange, $"Weather changed to {weather}");
            };
            
            System.Console.WriteLine("Journal and PoI systems created");
            
            // Initialize save system with compression disabled
            var performanceManager = new SavePerformanceManager(compressionEnabled: false);
            _saveManager = new SaveManager(performanceManager: performanceManager);
            _saveSlotManager = new SaveSlotManager();
            System.Console.WriteLine("SaveManager and SaveSlotManager created");
            
            // Initialize UI components
            _escapeMenuUI = new EscapeMenuUI(GraphicsDevice, SetGamePaused);
            _saveLoadUI = new SaveLoadUI(GraphicsDevice, _saveManager, _saveSlotManager);
            System.Console.WriteLine("Save/Load UI components created");
            
            // Register all ISaveable systems with SaveManager
            RegisterSaveableSystems();
            System.Console.WriteLine("ISaveable systems registered with SaveManager");
            
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
            
            // Load all assets
            _assetManager.LoadAssets();
            System.Console.WriteLine("Assets loaded");
            
            // Set up adventurer sprites
            _adventurer.LoadContent(
                _assetManager.GetTexture("adventurer"), 
                _assetManager.GetTexture("adventurer_walking")
            );
            System.Console.WriteLine("Adventurer content loaded");
            
            // Set up zone manager
            _zoneManager.LoadContent(_assetManager);
            System.Console.WriteLine("ZoneManager content loaded");
            
            // Set up PoI manager
            _poiManager.LoadContent();
            System.Console.WriteLine("PoI content loaded");
            
            // Now create UI manager with zone manager reference
            _uiManager = new UIManager(GraphicsDevice, _timeManager, _zoneManager, _weatherManager);
            System.Console.WriteLine("UIManager created");
            
            // Create minimap in top-right corner
            Rectangle minimapArea = new Rectangle(
                _graphics.PreferredBackBufferWidth - 220, 10, 
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
                System.Console.WriteLine("UI font and textures loaded successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load font: {ex.Message}");
                System.Console.WriteLine("UI will use fallback graphics instead of text");
                // Continue without font - UI will handle null font gracefully
            }
            
            // Load UI content
            try
            {
                var font = Content.Load<SpriteFont>("fonts/Arial");
                _escapeMenuUI.LoadContent(font);
                _saveLoadUI.LoadContent(font);
                System.Console.WriteLine("Save/Load UI content loaded");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load UI font: {ex.Message}");
                // Continue without font - UI will handle null font gracefully
            }
            
            // Wire up UI event handlers
            WireUpUIEventHandlers();
            
            // Initialize save system after all content is loaded
            InitializeSaveSystem();
            
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
            // Only exit on gamepad back button, not Escape key (Escape opens menu instead)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            // Handle keyboard input for zoom control
            var keyboardState = Keyboard.GetState();
            
            // Alt+F4 to exit (standard Windows shortcut)
            if (keyboardState.IsKeyDown(Keys.LeftAlt) && keyboardState.IsKeyDown(Keys.F4))
                Exit();
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
                
                // Check UI clicks in priority order
                bool uiHandledClick = false;
                
                // Check save/load UI first (highest priority when visible)
                if (_saveLoadUI?.IsVisible == true)
                {
                    uiHandledClick = _saveLoadUI.HandleMouseClick(mousePos);
                }
                
                // Check escape menu UI
                if (!uiHandledClick && _escapeMenuUI?.IsVisible == true)
                {
                    uiHandledClick = _escapeMenuUI.HandleMouseClick(mousePos);
                }
                
                // Check other UI elements
                if (!uiHandledClick)
                {
                    uiHandledClick = _uiManager.HandleMouseClick(mousePos);
                }
                
                // If no UI handled the click, convert to world coordinates for game interaction
                if (!uiHandledClick)
                {
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(mousePos);
                    // You can add logic here to influence the adventurer's direction toward the click
                }
            }
            _previousMouseState = currentMouseState;

            // Handle spacebar for pause toggle
            if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
            {
                _uiManager.SetPaused(!_uiManager.IsPaused);
            }
            
            // Handle save/load keyboard shortcuts
            if (keyboardState.IsKeyDown(Keys.F5) && !_previousKeyboardState.IsKeyDown(Keys.F5))
            {
                // F5 for quick save
                SaveGame(1);
            }
            if (keyboardState.IsKeyDown(Keys.F9) && !_previousKeyboardState.IsKeyDown(Keys.F9))
            {
                // F9 for quick load
                LoadGame(1);
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
            
            // Update UI systems (always update these)
            _escapeMenuUI?.Update(gameTime);
            _saveLoadUI?.Update(gameTime);
            
            // Update game systems
            if (_adventurer != null && _zoneManager != null)
            {
                // Always update journal UI (can be opened while paused)
                _journalUI.Update(gameTime);
                
                // Only update game simulation if not paused
                if (!_uiManager.IsPaused && !_escapeMenuUI.IsPaused)
                {
                    // Update time system
                    _timeManager.Update(gameTime);
                    
                    // Update weather system
                    _weatherManager.Update(gameTime);
                    
                    // Update weather effects
                    _weatherEffects.Update(gameTime, _weatherManager.CurrentWeather, _weatherManager.WeatherIntensity);
                    
                    // Update PoI system
                    _poiManager.Update(_adventurer.Position);
                    
                    // Update adventurer (this may trigger zone changes)
                    _adventurer.Update(gameTime, _zoneManager, _poiManager);
                    
                    // Check for zone changes BEFORE calling zoneManager.Update (which resets the flag)
                    bool zoneChanged = _zoneManager.ZoneChanged;
                    
                    _camera.Follow(_adventurer.Position);
                    _camera.Update(gameTime);
                    _zoneManager.Update(_adventurer.Position);
                    
                    // Update save system (handles auto-save timing)
                    _saveManager?.Update(gameTime);
                    
                    // Handle zone changes after all updates
                    if (zoneChanged && _miniMap != null)
                    {
                        // Start transition effect
                        _isTransitioning = true;
                        _transitionTimer = 0f;
                        
                        // Snap camera to new position immediately
                        _camera.SnapToPosition(_adventurer.Position);
                        
                        _miniMap.OnZoneChanged();
                        
                        // Record zone visit in journal
                        _journalManager.OnZoneEntered(_zoneManager.CurrentZone);
                        
                        // Generate PoIs for new zone
                        _poiManager.GeneratePoIsForZone(_zoneManager.CurrentZone, 32, 32);
                        
                        // Trigger auto-save on biome transition
                        _saveManager?.TriggerAutoSave(AutoSaveTrigger.BiomeTransition, $"Entered {_zoneManager.CurrentZone.Name} ({_zoneManager.CurrentZone.BiomeType})");
                        
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
            // Clear with navy blue background for areas outside zones
            GraphicsDevice.Clear(new Color(32, 26, 52)); // #201A34

            if (_spriteBatch != null && _zoneManager != null && _adventurer != null)
            {
                // Draw world with camera transform
                _spriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix(), samplerState: SamplerState.PointClamp);
                
                // Draw zone (terrain and objects)
                _zoneManager.Draw(_spriteBatch);
                
                // Draw PoIs (buildings, NPCs, etc.)
                _poiManager.Draw(_spriteBatch);
                
                // Draw adventurer (foreground)
                _adventurer.Draw(_spriteBatch);
                
                _spriteBatch.End();

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
                
                // Draw save/load UI components
                _escapeMenuUI?.Draw(_spriteBatch);
                _saveLoadUI?.Draw(_spriteBatch);
                
                _spriteBatch.End();
                
                // Apply subtle night tint
                if (_timeManager.CurrentTimeOfDay == TimeOfDay.Night)
                {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
                    Rectangle screenRect = new Rectangle(0, 0, 
                        _graphics.PreferredBackBufferWidth, 
                        _graphics.PreferredBackBufferHeight);
                    
                    // Subtle blue tint for night atmosphere
                    Color nightTint = new Color(100, 120, 180, 60); // Light blue with low alpha
                    _spriteBatch.Draw(_fadeTexture, screenRect, nightTint);
                    _spriteBatch.End();
                }
                
                // Apply weather effects
                Color weatherTint = _weatherManager.GetWeatherTint();
                if (weatherTint != Color.Transparent)
                {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
                    Rectangle screenRect = new Rectangle(0, 0, 
                        _graphics.PreferredBackBufferWidth, 
                        _graphics.PreferredBackBufferHeight);
                    
                    _spriteBatch.Draw(_fadeTexture, screenRect, weatherTint);
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
                    
                    Rectangle screenRect = new Rectangle(0, 0, 
                        _graphics.PreferredBackBufferWidth, 
                        _graphics.PreferredBackBufferHeight);
                    
                    _spriteBatch.Draw(_fadeTexture, screenRect, Color.Black * alpha);
                    _spriteBatch.End();
                }
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
    /// Registers all ISaveable systems with the SaveManager
    /// </summary>
    private void RegisterSaveableSystems()
    {
        try
        {
            _saveManager.RegisterSaveable(_adventurer);
            _saveManager.RegisterSaveable(_journalManager);
            _saveManager.RegisterSaveable(_poiManager);
            _saveManager.RegisterSaveable(_timeManager);
            _saveManager.RegisterSaveable(_weatherManager);
            _saveManager.RegisterSaveable(_zoneManager);
            
            System.Console.WriteLine($"Registered {_saveManager.RegisteredSystemCount} ISaveable systems");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error registering ISaveable systems: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Initializes the save system after content is loaded
    /// </summary>
    private void InitializeSaveSystem()
    {
        try
        {
            // Subscribe to save system events for user feedback
            _saveManager.SaveCompleted += OnSaveCompleted;
            _saveManager.LoadCompleted += OnLoadCompleted;
            _saveManager.SaveError += OnSaveError;
            _saveManager.AutoSaveTriggered += OnAutoSaveTriggered;
            
            System.Console.WriteLine("Save system initialized with event handlers");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error initializing save system: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Event handler for successful save operations
    /// </summary>
    private void OnSaveCompleted(object sender, SaveCompletedEventArgs e)
    {
        System.Console.WriteLine($"Save completed successfully for slot {e.SlotId} with {e.SystemCount} systems at {e.SaveTimestamp}");
    }

    /// <summary>
    /// Event handler for successful load operations
    /// </summary>
    private void OnLoadCompleted(object sender, LoadCompletedEventArgs e)
    {
        System.Console.WriteLine($"Load completed successfully for slot {e.SlotId} with {e.SystemCount} systems from {e.SaveTimestamp} (Game version: {e.GameVersion})");
    }

    /// <summary>
    /// Event handler for save/load errors
    /// </summary>
    private void OnSaveError(object sender, SaveErrorEventArgs e)
    {
        System.Console.WriteLine($"Save system error: {e.ErrorType} - {e.ErrorMessage}");
        if (e.Exception != null)
        {
            System.Console.WriteLine($"Exception details: {e.Exception}");
        }
    }

    /// <summary>
    /// Event handler for auto-save triggers
    /// </summary>
    private void OnAutoSaveTriggered(object sender, AutoSaveTriggeredEventArgs e)
    {
        System.Console.WriteLine($"Auto-save triggered: {e.Trigger} for slot {e.SlotId}");
        if (!string.IsNullOrEmpty(e.AdditionalInfo))
        {
            System.Console.WriteLine($"Additional info: {e.AdditionalInfo}");
        }
    }

    /// <summary>
    /// Performs a manual save to the specified slot
    /// </summary>
    /// <param name="slotId">The save slot to save to (1-based)</param>
    public async void SaveGame(int slotId = 1)
    {
        try
        {
            System.Console.WriteLine($"Starting manual save to slot {slotId}...");
            await _saveManager.SaveGameAsync(slotId);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Manual save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a game from the specified slot
    /// </summary>
    /// <param name="slotId">The save slot to load from (1-based)</param>
    public async void LoadGame(int slotId = 1)
    {
        try
        {
            System.Console.WriteLine($"Starting load from slot {slotId}...");
            await _saveManager.LoadGameAsync(slotId);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ LOAD FAILED - Slot {slotId}");
            System.Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Wires up event handlers for UI components
    /// </summary>
    private void WireUpUIEventHandlers()
    {
        // Escape menu event handlers
        _escapeMenuUI.ResumeRequested += (sender, e) => { /* Resume is handled automatically */ };
        _escapeMenuUI.SaveRequested += (sender, e) => _saveLoadUI.Show(true); // Show save UI
        _escapeMenuUI.LoadRequested += (sender, e) => _saveLoadUI.Show(false); // Show load UI
        _escapeMenuUI.OptionsRequested += (sender, e) => { /* TODO: Implement options menu */ };
        _escapeMenuUI.MainMenuRequested += (sender, e) => Exit(); // Exit game until main menu is implemented
        
        // Save/Load UI event handlers
        _saveLoadUI.SaveSlotSelected += OnSaveSlotSelected;
        _saveLoadUI.LoadSlotSelected += OnLoadSlotSelected;
        _saveLoadUI.NewGameRequested += OnNewGameRequested;
        _saveLoadUI.BackRequested += (sender, e) => _saveLoadUI.Hide();
    }

    /// <summary>
    /// Sets the game pause state
    /// </summary>
    /// <param name="paused">True to pause the game, false to resume</param>
    private void SetGamePaused(bool paused)
    {
        _uiManager?.SetPaused(paused);
    }

    /// <summary>
    /// Handles save slot selection from the save UI
    /// </summary>
    private async void OnSaveSlotSelected(object sender, SaveSlotSelectedEventArgs e)
    {
        try
        {
            System.Console.WriteLine($"🎯 OnSaveSlotSelected called for slot {e.SlotId}");
            System.Console.WriteLine($"📊 About to call SaveGameAsync...");
            await _saveManager.SaveGameAsync(e.SlotId);
            System.Console.WriteLine($"✅ SaveGameAsync completed successfully");
            
            System.Console.WriteLine($"📊 About to update slot metadata...");
            // Update slot metadata after successful save
            await UpdateSlotMetadataAfterSave(e.SlotId);
            System.Console.WriteLine($"✅ Metadata update completed successfully");
            
            _saveLoadUI.Hide();
            _escapeMenuUI.HideMenu();
            System.Console.WriteLine($"✅ Save operation completed for slot {e.SlotId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ SAVE FAILED - Slot {e.SlotId}");
            System.Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Handles load slot selection from the load UI
    /// </summary>
    private async void OnLoadSlotSelected(object sender, LoadSlotSelectedEventArgs e)
    {
        try
        {
            System.Console.WriteLine($"🎯 OnLoadSlotSelected called for slot {e.SlotId}");
            System.Console.WriteLine($"📊 Before load - Adventurer position: {_adventurer.Position}, Day: {_timeManager.CurrentDay}");
            
            System.Console.WriteLine($"📊 About to call LoadGameAsync...");
            await _saveManager.LoadGameAsync(e.SlotId);
            System.Console.WriteLine($"✅ LoadGameAsync completed successfully");
            
            System.Console.WriteLine($"📊 After load - Adventurer position: {_adventurer.Position}, Day: {_timeManager.CurrentDay}");
            
            _saveLoadUI.Hide();
            _escapeMenuUI.HideMenu();
            
            System.Console.WriteLine($"✅ Load operation completed for slot {e.SlotId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ LOAD FAILED - Slot {e.SlotId}");
            System.Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Handles new game request from the load UI
    /// </summary>
    private void OnNewGameRequested(object sender, EventArgs e)
    {
        // TODO: Implement new game functionality
        // For now, just hide the UI
        _saveLoadUI.Hide();
        _escapeMenuUI.HideMenu();
        System.Console.WriteLine("New game requested - not yet implemented");
    }

    /// <summary>
    /// Updates slot metadata after a successful save operation
    /// </summary>
    /// <param name="slotId">The slot ID that was saved to</param>
    private async Task UpdateSlotMetadataAfterSave(int slotId)
    {
        try
        {
            // Get journal statistics for metadata
            var journalStats = _journalManager.GetStatistics();
            
            // Create or update slot metadata with current game state
            var metadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                LastSaveTime = DateTime.UtcNow,
                PlayTime = TimeSpan.FromSeconds(_timeManager.CurrentTime), // Use CurrentTime as total play time
                CurrentDay = _timeManager.CurrentDay,
                CurrentZoneName = _zoneManager.CurrentZone?.Name ?? "Unknown Zone",
                CurrentBiome = _zoneManager.CurrentZone?.BiomeType ?? BiomeType.Plains,
                ZonesVisited = journalStats.ZonesVisited,
                JournalEntries = journalStats.TotalEntries,
                GameVersion = "1.0.0", // TODO: Get from assembly version
                FileSizeBytes = 0 // Will be updated by SaveSlotManager
            };

            // Create slot if it doesn't exist, or update existing metadata
            if (!_saveSlotManager.SlotExists(slotId))
            {
                await _saveSlotManager.CreateSlotAsync(slotId, metadata);
            }
            else
            {
                await _saveSlotManager.UpdateSlotMetadataAsync(slotId, metadata);
            }

            System.Console.WriteLine($"Updated metadata for slot {slotId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to update slot metadata for slot {slotId}: {ex.Message}");
        }
    }

    protected override void UnloadContent()
    {
        // Cleanup save system
        if (_saveManager != null)
        {
            _saveManager.SaveCompleted -= OnSaveCompleted;
            _saveManager.LoadCompleted -= OnLoadCompleted;
            _saveManager.SaveError -= OnSaveError;
            _saveManager.AutoSaveTriggered -= OnAutoSaveTriggered;
        }
        
        _assetManager?.UnloadAssets();
        _miniMap?.Dispose();
        _uiManager?.Dispose();
        _weatherEffects?.Dispose();
        _journalUI?.Dispose();
        _escapeMenuUI?.Dispose();
        _saveLoadUI?.Dispose();
        _fadeTexture?.Dispose();
        base.UnloadContent();
    }
}