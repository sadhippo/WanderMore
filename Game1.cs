using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HiddenHorizons;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    // Game systems
    private AssetManager _assetManager;
    private Adventurer _adventurer;
    private Camera _camera;
    private TerrainGenerator _terrainGenerator;
    
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
        // Initialize game systems
        _assetManager = new AssetManager(Content);
        _adventurer = new Adventurer(new Vector2(100, 100));
        _camera = new Camera(GraphicsDevice.Viewport);
        _terrainGenerator = new TerrainGenerator(12345); // Fixed seed for consistent world
        
        _previousMouseState = Mouse.GetState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Load all assets
        _assetManager.LoadAssets();
        
        // Set up adventurer sprite
        _adventurer.LoadContent(_assetManager.GetTexture("adventurer"));
        
        // Set up terrain textures
        _terrainGenerator.LoadContent(
            _assetManager.GetTexture("grass"),
            _assetManager.GetTexture("stone"),
            _assetManager.GetTexture("water")
        );
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

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

        // Update game systems
        _adventurer.Update(gameTime);
        _camera.Follow(_adventurer.Position);
        _camera.Update(gameTime);
        _terrainGenerator.Update(_adventurer.Position);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.SkyBlue);

        // Draw world with camera transform
        _spriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix(), samplerState: SamplerState.PointClamp);
        
        // Draw terrain first (background)
        _terrainGenerator.Draw(_spriteBatch, _camera);
        
        // Draw adventurer
        _adventurer.Draw(_spriteBatch);
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _assetManager?.UnloadAssets();
        base.UnloadContent();
    }
}
