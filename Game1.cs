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
        
        // Set window size for comfortable viewing
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
    }

    protected override void Initialize()
    {
        try
        {
            System.Console.WriteLine("Initializing game systems...");
            
            // Initialize PoI tracking
            _approachedPoIs = new HashSet<Guid>();
            
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
            System.Console.WriteLine("StatsManager created and initialized");
            
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
            
            // Load all assets
            _assetManager.LoadAssets();
            System.Console.WriteLine("Assets loaded");
            
            // Load journal entry data
            JournalEntryData.Instance.LoadContent(Content);
            System.Console.WriteLine("Journal entry data loaded");
            
            // Set up adventurer sprites
            _adventurer.LoadContent(
                _assetManager.GetTexture("adventurer"), 
                _assetManager.GetTexture("adventurer_walking")
            );
            
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
            _uiManager.UpdateScreenSize(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            System.Console.WriteLine("UIManager created");
            
            // Create stats page
            _statsPage = new StatsPage(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            System.Console.WriteLine("StatsPage created");
            
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
                
                // Check UI clicks first (pause button, etc.)
                if (!_uiManager.HandleMouseClick(mousePos))
                {
                    // If UI didn't handle it, convert to world coordinates for game interaction
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(mousePos);
                    // You can add logic here to influence the adventurer's direction toward the click
                }
            }
            
            // Handle mouse hover for tooltips
            Vector2 currentMousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
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
                    
                    // Update weather system
                    _weatherManager.Update(gameTime);
                    
                    // Update weather effects
                    _weatherEffects.Update(gameTime, _weatherManager.CurrentWeather, _weatherManager.WeatherIntensity);
                    
                    // Update PoI system
                    _poiManager.Update(_adventurer.Position, 32f, _zoneManager.CurrentZone?.Id);
                    
                    // Update stats system
                    _statsManager.Update(gameTime);
                    
                    // Update UI manager (for ticker animations)
                    _uiManager.Update(gameTime);
                    
                    // Update adventurer (this may trigger zone changes)
                    _adventurer.Update(gameTime, _zoneManager, _poiManager, _questManager);
                    
                    // Check for zone changes BEFORE calling zoneManager.Update (which resets the flag)
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
            // Clear with navy blue background for areas outside zones
            GraphicsDevice.Clear(new Color(32, 26, 52)); // #201A34

            if (_spriteBatch != null && _zoneManager != null && _adventurer != null)
            {
                // Draw world with camera transform
                _spriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix(), samplerState: SamplerState.PointClamp);
                
                // Draw zone (terrain and objects)
                _zoneManager.Draw(_spriteBatch);
                
                // Draw PoIs (buildings, NPCs, etc.)
                _poiManager.Draw(_spriteBatch, _zoneManager.CurrentZone?.Id);
                
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
                
                // Draw stats page if open
                if (_statsPage != null && _statsManager != null)
                {
                    _statsPage.Draw(_spriteBatch, _statsManager.CurrentStats);
                }
                
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

    protected override void UnloadContent()
    {
        _assetManager?.UnloadAssets();
        _miniMap?.Dispose();
        _uiManager?.Dispose();
        _weatherEffects?.Dispose();
        _journalUI?.Dispose();
        _statsPage?.Dispose();
        _statsManager?.Dispose();
        _fadeTexture?.Dispose();
        base.UnloadContent();
    }
}