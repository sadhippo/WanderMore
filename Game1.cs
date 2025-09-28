using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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
    
    // Zone transition effects
    private bool _isTransitioning;
    private float _transitionTimer;
    private float _transitionDuration = 0.5f; // 0.5 seconds
    private Texture2D _fadeTexture;
    
    // Input handling
    private MouseState _previousMouseState;

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
            
            // Subscribe to weather changes for journal tracking
            _weatherManager.WeatherChanged += (weather) => {
                _journalManager.OnWeatherChanged(weather, _timeManager.GetSeasonName());
            };
            
            System.Console.WriteLine("Journal and PoI systems created");
            
            // Initialize UI system (will be created after zone manager loads)
            System.Console.WriteLine("UIManager will be created after zone loading");
            
            _previousMouseState = Mouse.GetState();

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
                _journalUI.LoadContent(font);
                System.Console.WriteLine("UI font loaded successfully");
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

            // Handle mouse input for occasional interaction
            var currentMouseState = Mouse.GetState();
            if (currentMouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                // Convert mouse position to world coordinates and influence adventurer
                Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
                // You can add logic here to influence the adventurer's direction toward the click
            }
            _previousMouseState = currentMouseState;

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
                // Update time system
                _timeManager.Update(gameTime);
                
                // Update weather system
                _weatherManager.Update(gameTime);
                
                // Update weather effects
                _weatherEffects.Update(gameTime, _weatherManager.CurrentWeather, _weatherManager.WeatherIntensity);
                
                // Update journal UI
                _journalUI.Update(gameTime);
                
                // Update PoI system
                _poiManager.Update(_adventurer.Position);
                
                // Update adventurer (this may trigger zone changes)
                _adventurer.Update(gameTime, _zoneManager, _poiManager);
                
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
                    
                    // Record zone visit in journal
                    _journalManager.OnZoneEntered(_zoneManager.CurrentZone);
                    
                    // Generate PoIs for new zone
                    _poiManager.GeneratePoIsForZone(_zoneManager.CurrentZone, 32, 32);
                    
                    System.Console.WriteLine($"Zone transition started to: {_zoneManager.CurrentZone.Name}");
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
        _fadeTexture?.Dispose();
        base.UnloadContent();
    }
}