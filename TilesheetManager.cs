using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HiddenHorizons
{
    public class TilesheetManager
    {
        private Dictionary<string, TilesheetData> _tilesheets;
        private Dictionary<string, Rectangle> _tileDefinitions;

        public TilesheetManager()
        {
            _tilesheets = new Dictionary<string, TilesheetData>();
            _tileDefinitions = new Dictionary<string, Rectangle>();
        }

        public void LoadTilesheet(string name, Texture2D texture, int tileWidth, int tileHeight)
        {
            _tilesheets[name] = new TilesheetData
            {
                Texture = texture,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                TilesWide = texture.Width / tileWidth,
                TilesHigh = texture.Height / tileHeight
            };
        }

        // Define individual tiles by their position in the sheet
        public void DefineTile(string tileName, string sheetName, int tileX, int tileY)
        {
            if (_tilesheets.TryGetValue(sheetName, out TilesheetData sheet))
            {
                _tileDefinitions[tileName] = new Rectangle(
                    tileX * sheet.TileWidth,
                    tileY * sheet.TileHeight,
                    sheet.TileWidth,
                    sheet.TileHeight
                );
            }
        }

        // Define a range of tiles (useful for animations or similar tiles)
        public void DefineTileRange(string baseName, string sheetName, int startX, int startY, int count, bool horizontal = true)
        {
            if (_tilesheets.TryGetValue(sheetName, out TilesheetData sheet))
            {
                for (int i = 0; i < count; i++)
                {
                    int tileX = horizontal ? startX + i : startX;
                    int tileY = horizontal ? startY : startY + i;
                    
                    _tileDefinitions[$"{baseName}_{i}"] = new Rectangle(
                        tileX * sheet.TileWidth,
                        tileY * sheet.TileHeight,
                        sheet.TileWidth,
                        sheet.TileHeight
                    );
                }
            }
        }

        public void DrawTile(SpriteBatch spriteBatch, string tileName, string sheetName, Vector2 position, Color color)
        {
            if (_tilesheets.TryGetValue(sheetName, out TilesheetData sheet) &&
                _tileDefinitions.TryGetValue(tileName, out Rectangle sourceRect))
            {
                spriteBatch.Draw(sheet.Texture, position, sourceRect, color);
            }
        }

        public void DrawTile(SpriteBatch spriteBatch, string tileName, string sheetName, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects)
        {
            if (_tilesheets.TryGetValue(sheetName, out TilesheetData sheet) &&
                _tileDefinitions.TryGetValue(tileName, out Rectangle sourceRect))
            {
                spriteBatch.Draw(sheet.Texture, position, sourceRect, color, rotation, origin, scale, effects, 0f);
            }
        }

        public Rectangle? GetTileRect(string tileName)
        {
            return _tileDefinitions.TryGetValue(tileName, out Rectangle rect) ? rect : null;
        }

        public Texture2D GetSheet(string sheetName)
        {
            return _tilesheets.TryGetValue(sheetName, out TilesheetData sheet) ? sheet.Texture : null;
        }

        private class TilesheetData
        {
            public Texture2D Texture { get; set; }
            public int TileWidth { get; set; }
            public int TileHeight { get; set; }
            public int TilesWide { get; set; }
            public int TilesHigh { get; set; }
        }
    }
}
