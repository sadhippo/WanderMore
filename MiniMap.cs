using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HiddenHorizons;

public class MiniMap
{
    private Texture2D _pixelTexture;
    private Rectangle _mapArea;
    private float _scale;
    private ZoneManager _zoneManager;

    public MiniMap(GraphicsDevice graphicsDevice, Rectangle mapArea, ZoneManager zoneManager)
    {
        _mapArea = mapArea;
        _zoneManager = zoneManager;
        
        // Create a 1x1 white pixel texture for drawing
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Calculate scale to fit zone in minimap area
        UpdateScale();
    }

    public void OnZoneChanged()
    {
        // Recalculate scale when zone changes
        UpdateScale();
    }

    private void UpdateScale()
    {
        if (_zoneManager.CurrentZone != null)
        {
            float scaleX = (float)_mapArea.Width / _zoneManager.CurrentZone.Width;
            float scaleY = (float)_mapArea.Height / _zoneManager.CurrentZone.Height;
            _scale = MathF.Min(scaleX, scaleY) * 0.9f; // Leave some padding
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 playerPosition)
    {
        if (_zoneManager.CurrentZone == null) return;

        var zone = _zoneManager.CurrentZone;
        
        // Draw background with navy blue
        spriteBatch.Draw(_pixelTexture, _mapArea, new Color(32, 26, 52) * 0.8f);
        
        // Draw border
        DrawBorder(spriteBatch, _mapArea, Color.White, 2);
        
        // Calculate minimap offset to center the zone
        Vector2 mapOffset = new Vector2(
            _mapArea.X + (_mapArea.Width - zone.Width * _scale) / 2,
            _mapArea.Y + (_mapArea.Height - zone.Height * _scale) / 2
        );

        // Draw explored tiles
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                if (zone.ExploredTiles[x, y])
                {
                    Vector2 tilePos = mapOffset + new Vector2(x * _scale, y * _scale);
                    Rectangle tileRect = new Rectangle(
                        (int)tilePos.X, (int)tilePos.Y, 
                        (int)MathF.Max(1, _scale), (int)MathF.Max(1, _scale)
                    );
                    
                    Color tileColor = GetTerrainColor(zone.Terrain[x, y]);
                    spriteBatch.Draw(_pixelTexture, tileRect, tileColor);
                }
                else
                {
                    // Draw unexplored areas as dark gray
                    Vector2 tilePos = mapOffset + new Vector2(x * _scale, y * _scale);
                    Rectangle tileRect = new Rectangle(
                        (int)tilePos.X, (int)tilePos.Y, 
                        (int)MathF.Max(1, _scale), (int)MathF.Max(1, _scale)
                    );
                    spriteBatch.Draw(_pixelTexture, tileRect, Color.DarkGray * 0.3f);
                }
            }
        }

        // Draw objects on explored tiles
        foreach (var obj in zone.Objects)
        {
            int objTileX = (int)(obj.Position.X / 32);
            int objTileY = (int)(obj.Position.Y / 32);
            
            if (objTileX >= 0 && objTileX < zone.Width && 
                objTileY >= 0 && objTileY < zone.Height &&
                zone.ExploredTiles[objTileX, objTileY])
            {
                Vector2 objPos = mapOffset + new Vector2(objTileX * _scale, objTileY * _scale);
                Rectangle objRect = new Rectangle(
                    (int)objPos.X, (int)objPos.Y, 
                    (int)MathF.Max(2, _scale), (int)MathF.Max(2, _scale)
                );
                
                Color objColor = GetObjectColor(obj.Type);
                spriteBatch.Draw(_pixelTexture, objRect, objColor);
            }
        }

        // Draw player position
        int playerTileX = (int)(playerPosition.X / 32);
        int playerTileY = (int)(playerPosition.Y / 32);
        
        if (playerTileX >= 0 && playerTileX < zone.Width && 
            playerTileY >= 0 && playerTileY < zone.Height)
        {
            Vector2 playerPos = mapOffset + new Vector2(playerTileX * _scale, playerTileY * _scale);
            Rectangle playerRect = new Rectangle(
                (int)playerPos.X - 1, (int)playerPos.Y - 1, 
                (int)MathF.Max(3, _scale + 2), (int)MathF.Max(3, _scale + 2)
            );
            spriteBatch.Draw(_pixelTexture, playerRect, Color.Yellow);
        }

        // Draw zone info
        DrawZoneInfo(spriteBatch, zone);
    }

    private Color GetTerrainColor(TerrainType terrainType)
    {
        return terrainType switch
        {
            TerrainType.Grass => Color.LightGreen,
            TerrainType.Dirt => Color.SaddleBrown,
            TerrainType.Water => Color.CornflowerBlue,
            TerrainType.Stone => Color.Gray,
            _ => Color.Purple
        };
    }

    private Color GetObjectColor(ObjectType objectType)
    {
        return objectType switch
        {
            ObjectType.SingleTree => Color.DarkGreen,
            ObjectType.DoubleTree => Color.ForestGreen,
            ObjectType.Rock => Color.DarkGray,
            ObjectType.Plant => Color.Green,
            ObjectType.Bush => Color.OliveDrab,
            _ => Color.White
        };
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Y, area.Width, thickness), color);
        // Bottom  
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Y, thickness, area.Height), color);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
    }

    private void DrawZoneInfo(SpriteBatch spriteBatch, Zone zone)
    {
        // This would need a SpriteFont to draw text
        // For now, we'll just draw a colored indicator for the zone type
        Rectangle zoneIndicator = new Rectangle(_mapArea.X + 5, _mapArea.Y + 5, 20, 20);
        Color zoneColor = zone.BiomeType switch
        {
            BiomeType.Forest => Color.Green,
            BiomeType.Lake => Color.Blue,
            BiomeType.Mountain => Color.Gray,
            BiomeType.DenseForest => Color.DarkGreen,
            BiomeType.Plains => Color.Yellow,
            BiomeType.Swamp => Color.DarkOliveGreen,
            _ => Color.White
        };
        spriteBatch.Draw(_pixelTexture, zoneIndicator, zoneColor);
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}