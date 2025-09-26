using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HiddenHorizons;

public class AssetManager
{
    private ContentManager _content;
    private Dictionary<string, Texture2D> _textures;

    public AssetManager(ContentManager content)
    {
        _content = content;
        _textures = new Dictionary<string, Texture2D>();
    }

    public void LoadAssets()
    {
        // Create placeholder textures for now
        // You can replace these with actual asset loading later
        CreatePlaceholderTextures();
    }

    private void CreatePlaceholderTextures()
    {
        var graphicsDevice = _content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
        
        if (graphicsDevice?.GraphicsDevice != null)
        {
            // Create simple colored rectangles as placeholders
            _textures["adventurer"] = CreateColoredTexture(graphicsDevice.GraphicsDevice, 16, 16, Microsoft.Xna.Framework.Color.Blue);
            _textures["grass"] = CreateColoredTexture(graphicsDevice.GraphicsDevice, 16, 16, Microsoft.Xna.Framework.Color.Green);
            _textures["stone"] = CreateColoredTexture(graphicsDevice.GraphicsDevice, 16, 16, Microsoft.Xna.Framework.Color.Gray);
            _textures["water"] = CreateColoredTexture(graphicsDevice.GraphicsDevice, 16, 16, Microsoft.Xna.Framework.Color.Blue);
        }
    }

    private Texture2D CreateColoredTexture(GraphicsDevice graphicsDevice, int width, int height, Microsoft.Xna.Framework.Color color)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var colorData = new Microsoft.Xna.Framework.Color[width * height];
        
        for (int i = 0; i < colorData.Length; i++)
        {
            colorData[i] = color;
        }
        
        texture.SetData(colorData);
        return texture;
    }

    public Texture2D GetTexture(string name)
    {
        return _textures.TryGetValue(name, out var texture) ? texture : null;
    }

    public void LoadTexture(string name, string assetPath)
    {
        try
        {
            _textures[name] = _content.Load<Texture2D>(assetPath);
        }
        catch
        {
            // If loading fails, keep the placeholder or create a new one
            System.Console.WriteLine($"Failed to load texture: {assetPath}");
        }
    }

    public void UnloadAssets()
    {
        foreach (var texture in _textures.Values)
        {
            texture?.Dispose();
        }
        _textures.Clear();
    }
}