using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HiddenHorizons;

public enum AspectRatioMode
{
    Regular,    // 16:9 or similar
    TikTok      // 9:16 vertical
}

public class VirtualResolution
{
    private GraphicsDeviceManager _graphics;
    private GraphicsDevice _graphicsDevice;
    private Game _game;
    
    // Virtual resolution - the game is designed for this resolution
    public int VirtualWidth { get; private set; }
    public int VirtualHeight { get; private set; }
    
    // Actual screen resolution
    public int ScreenWidth => _graphics.PreferredBackBufferWidth;
    public int ScreenHeight => _graphics.PreferredBackBufferHeight;
    
    // Current aspect ratio mode
    public AspectRatioMode CurrentMode { get; private set; } = AspectRatioMode.Regular;
    
    // Scaling and positioning for letterboxing/pillarboxing
    public Matrix ScaleMatrix { get; private set; }
    public Rectangle RenderRectangle { get; private set; }
    
    // Render target for virtual resolution
    private RenderTarget2D _virtualRenderTarget;
    
    public VirtualResolution(GraphicsDeviceManager graphics, GraphicsDevice graphicsDevice, Game game,
                           int virtualWidth = 1024, int virtualHeight = 768)
    {
        _graphics = graphics;
        _graphicsDevice = graphicsDevice;
        _game = game;
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;
        
        UpdateResolution();
    }
    
    public void SetAspectRatioMode(AspectRatioMode mode)
    {
        if (CurrentMode == mode) return;
        
        CurrentMode = mode;
        
        switch (mode)
        {
            case AspectRatioMode.Regular:
                // Standard landscape gaming resolution
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = 1024;
                _graphics.PreferredBackBufferHeight = 768;
                // Keep original virtual resolution
                VirtualWidth = 1024;
                VirtualHeight = 768;
                break;
                
            case AspectRatioMode.TikTok:
                // 9:16 aspect ratio for vertical streaming
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = 576;  // 9 units
                _graphics.PreferredBackBufferHeight = 1024; // 16 units
                // Adjust virtual resolution to fill the 9:16 space
                VirtualWidth = 576;
                VirtualHeight = 1024;
                break;
        }
        
        _graphics.ApplyChanges();
        UpdateResolution();
    }
    
    public void UpdateResolution()
    {
        // Recreate render target if needed
        if (_virtualRenderTarget == null || 
            _virtualRenderTarget.Width != VirtualWidth || 
            _virtualRenderTarget.Height != VirtualHeight)
        {
            _virtualRenderTarget?.Dispose();
            _virtualRenderTarget = new RenderTarget2D(_graphicsDevice, VirtualWidth, VirtualHeight);
        }
        
        CalculateScaling();
    }
    
    private void CalculateScaling()
    {
        float screenAspect = (float)ScreenWidth / ScreenHeight;
        float virtualAspect = (float)VirtualWidth / VirtualHeight;
        
        float scale;
        int renderWidth, renderHeight;
        int offsetX = 0, offsetY = 0;
        
        if (screenAspect > virtualAspect)
        {
            // Screen is wider than virtual resolution - pillarbox (black bars on sides)
            scale = (float)ScreenHeight / VirtualHeight;
            renderWidth = (int)(VirtualWidth * scale);
            renderHeight = ScreenHeight;
            offsetX = (ScreenWidth - renderWidth) / 2;
        }
        else
        {
            // Screen is taller than virtual resolution - letterbox (black bars on top/bottom)
            scale = (float)ScreenWidth / VirtualWidth;
            renderWidth = ScreenWidth;
            renderHeight = (int)(VirtualHeight * scale);
            offsetY = (ScreenHeight - renderHeight) / 2;
        }
        
        RenderRectangle = new Rectangle(offsetX, offsetY, renderWidth, renderHeight);
        
        // Create scale matrix for UI positioning
        ScaleMatrix = Matrix.CreateScale(scale);
    }
    
    public void BeginVirtualDraw()
    {
        _graphicsDevice.SetRenderTarget(_virtualRenderTarget);
        _graphicsDevice.Clear(new Color(32, 26, 52)); // Same background as main game
    }
    
    public void EndVirtualDraw()
    {
        _graphicsDevice.SetRenderTarget(null);
    }
    
    public void DrawVirtualToScreen(SpriteBatch spriteBatch)
    {
        // Clear screen with black (for letterbox/pillarbox areas)
        _graphicsDevice.Clear(Color.Black);
        
        // Draw the virtual render target to the calculated rectangle
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
        spriteBatch.Draw(_virtualRenderTarget, RenderRectangle, Color.White);
        spriteBatch.End();
    }
    
    public Vector2 ScreenToVirtual(Vector2 screenPosition)
    {
        // Convert screen coordinates to virtual coordinates
        Vector2 relativePos = screenPosition - new Vector2(RenderRectangle.X, RenderRectangle.Y);
        float scaleX = (float)VirtualWidth / RenderRectangle.Width;
        float scaleY = (float)VirtualHeight / RenderRectangle.Height;
        
        return new Vector2(relativePos.X * scaleX, relativePos.Y * scaleY);
    }
    
    public Vector2 VirtualToScreen(Vector2 virtualPosition)
    {
        // Convert virtual coordinates to screen coordinates
        float scaleX = (float)RenderRectangle.Width / VirtualWidth;
        float scaleY = (float)RenderRectangle.Height / VirtualHeight;
        
        return new Vector2(
            virtualPosition.X * scaleX + RenderRectangle.X,
            virtualPosition.Y * scaleY + RenderRectangle.Y
        );
    }
    
    public Matrix GetVirtualTransformMatrix()
    {
        // Returns a matrix for transforming from virtual to screen space
        return Matrix.CreateTranslation(-RenderRectangle.X, -RenderRectangle.Y, 0) *
               Matrix.CreateScale((float)VirtualWidth / RenderRectangle.Width, 
                                (float)VirtualHeight / RenderRectangle.Height, 1f);
    }
    
    public Viewport GetVirtualViewport()
    {
        return new Viewport(0, 0, VirtualWidth, VirtualHeight);
    }
    
    public void Dispose()
    {
        _virtualRenderTarget?.Dispose();
    }
}
