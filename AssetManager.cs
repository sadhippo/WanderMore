// Updated AssetManager.cs - Now uses tilesheets
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HiddenHorizons
{
    public class AssetManager
    {
        private ContentManager _content;
        private TilesheetManager _tilesheetManager;
        private Dictionary<string, Texture2D> _individualTextures; // For non-tilesheet assets

        public AssetManager(ContentManager content)
        {
            _content = content;
            _tilesheetManager = new TilesheetManager();
            _individualTextures = new Dictionary<string, Texture2D>();
        }

        public ContentManager GetContent() => _content;

        public void LoadAssets()
        {
            // Load your main fantasy tilesheet
            try
            {
                var fantasySheet = _content.Load<Texture2D>("tilesheet/fantasytilesheet");
                _tilesheetManager.LoadTilesheet("fantasy", fantasySheet, 32, 32); // 16x16 tiles based on your coordinates
                
                // Define specific tiles from your fantasy sheet
                DefineTerrainTiles();
            }
            catch
            {
                // Fallback to placeholder system
                CreatePlaceholderTilesheets();
            }
            //load objects
            try
            {
                var fantasySheetobjects = _content.Load<Texture2D>("tilesheet/fantasytilesheet_objects");
                _tilesheetManager.LoadTilesheet("fantasyObjects", fantasySheetobjects, 32, 32); // 16x16 tiles based on your coordinates
                
                // Define specific tiles from your fantasy sheet
                DefineObjectTiles();
            }
            catch
            {
                // Fallback to placeholder system
                CreatePlaceholderTilesheets();
            }
            // Load adventurer sprites
            try
            {
                var adventurerTexture = _content.Load<Texture2D>("adventurer/homeboy");
                _individualTextures["adventurer"] = adventurerTexture;
                
                var walkingTexture = _content.Load<Texture2D>("adventurer/homeboywalkingsmall");
                _individualTextures["adventurer_walking"] = walkingTexture;
            }
            catch
            {
                // Create placeholder adventurer if loading fails
                CreatePlaceholderAdventurer();
            }
        }

        private void DefineTerrainTiles()
        {
            _tilesheetManager.DefineTile("grass", "fantasy", 4, 11);
            _tilesheetManager.DefineTile("water", "fantasy", 4, 14);
            _tilesheetManager.DefineTile("dirt",  "fantasy", 5, 11);
            _tilesheetManager.DefineTile("stone", "fantasy", 6, 11);
        }

        private void DefineObjectTiles()
        {
            _tilesheetManager.DefineTile("single_tree", "fantasyObjects", 4, 5);
            _tilesheetManager.DefineTile("double_tree", "fantasyObjects", 4, 6);
            _tilesheetManager.DefineTile("rock",        "fantasyObjects", 4, 10);
            _tilesheetManager.DefineTile("plant",       "fantasyObjects", 4, 8);
            _tilesheetManager.DefineTile("bush",        "fantasyObjects", 4, 9);
        }


        private void CreatePlaceholderAdventurer()
        {
            var graphicsDevice = _content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            
            if (graphicsDevice?.GraphicsDevice != null)
            {
                _individualTextures["adventurer"] = CreateColoredTexture(graphicsDevice.GraphicsDevice, 32, 32, Color.Blue);
                _individualTextures["adventurer_walking"] = CreateColoredTexture(graphicsDevice.GraphicsDevice, 32, 96, Color.DarkBlue);
            }
        }

        private void LoadAdditionalTilesheets()
        {
            // If you get more tilesheets for other biomes, load them here
            try
            {
                // Example for future expansion
                // var forestSheet = _content.Load<Texture2D>("Tilesheets/ForestTilesheet");
                // _tilesheetManager.LoadTilesheet("forest", forestSheet, 32, 32);
                // DefineTilesFromForestSheet();
            }
            catch
            {
                // Handle missing sheets gracefully
            }
        }

        private void CreatePlaceholderTilesheets()
        {
            // Create a simple placeholder tilesheet for development
            var graphicsDevice = _content.ServiceProvider.GetService(typeof(GraphicsDevice)) as GraphicsDevice;
            
            // Create a 8x8 tilesheet with different colored tiles
            var placeholderSheet = new Texture2D(graphicsDevice, 256, 256); // 8x8 tiles of 32x32
            Color[] colors = new Color[256 * 256];
            
            // Fill with different colored tiles
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    Color tileColor = GetPlaceholderColor(x, y);
                    FillTileArea(colors, x, y, tileColor);
                }
            }
            
            placeholderSheet.SetData(colors);
            _tilesheetManager.LoadTilesheet("fantasy", placeholderSheet, 32, 32);
            
            // Define placeholder tiles
            _tilesheetManager.DefineTile("grass", "fantasy", 0, 0);
            _tilesheetManager.DefineTile("dirt", "fantasy", 1, 0);
            _tilesheetManager.DefineTile("stone", "fantasy", 2, 0);
            _tilesheetManager.DefineTile("water", "fantasy", 3, 0);
            _tilesheetManager.DefineTile("single_tree", "fantasy", 0, 1);
            _tilesheetManager.DefineTile("double_tree", "fantasy", 1, 1);
            _tilesheetManager.DefineTile("rock", "fantasy", 2, 1);
            _tilesheetManager.DefineTileRange("adventurer", "fantasy", 0, 2, 4, true);
        }

        private void FillTileArea(Color[] colors, int tileX, int tileY, Color color)
        {
            for (int py = 0; py < 32; py++)
            {
                for (int px = 0; px < 32; px++)
                {
                    int x = tileX * 32 + px;
                    int y = tileY * 32 + py;
                    colors[y * 256 + x] = color;
                }
            }
        }

        private Color GetPlaceholderColor(int tileX, int tileY)
        {
            return (tileX + tileY * 8) switch
            {
                0 => Color.Green,       // grass
                1 => Color.SaddleBrown, // dirt  
                2 => Color.Gray,        // stone
                3 => Color.Blue,        // water
                8 => Color.DarkGreen,   // single tree
                9 => Color.ForestGreen, // double tree
                10 => Color.DarkGray,   // rock
                16 => Color.Blue,       // adventurer frame 1
                17 => Color.DarkBlue,   // adventurer frame 2
                18 => Color.Navy,       // adventurer frame 3
                19 => Color.CornflowerBlue, // adventurer frame 4
                _ => Color.Purple
            };
        }

        private Texture2D CreateColoredTexture(GraphicsDevice graphicsDevice, int width, int height, Color color)
        {
            var texture = new Texture2D(graphicsDevice, width, height);
            var colorData = new Color[width * height];
            
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = color;
            }
            
            texture.SetData(colorData);
            return texture;
        }

        public void UnloadAssets()
        {
            foreach (var texture in _individualTextures.Values)
            {
                texture?.Dispose();
            }
            _individualTextures.Clear();
        }

        // Updated methods to use tilesheet system
        public void DrawTerrain(SpriteBatch spriteBatch, TerrainType terrainType, Vector2 position)
        {
            string tileName = terrainType switch
            {
                TerrainType.Grass => "grass",
                TerrainType.Dirt => "dirt", 
                TerrainType.Water => "water",
                TerrainType.Stone => "stone",
                _ => "grass"
            };
            
            _tilesheetManager.DrawTile(spriteBatch, tileName, "fantasy", position, Color.White);
        }

        public void DrawObject(SpriteBatch spriteBatch, ObjectType objectType, Vector2 position)
        {
            string tileName = objectType switch
            {
                ObjectType.SingleTree => "single_tree",
                ObjectType.DoubleTree => "double_tree",
                ObjectType.Rock => "rock",
                ObjectType.Plant => "plant",
                ObjectType.Bush => "bush",
                _ => "single_tree"
            };
            
            _tilesheetManager.DrawTile(spriteBatch, tileName, "fantasyObjects", position, Color.White);
        }

        public void DrawAdventurer(SpriteBatch spriteBatch, Vector2 position, int frameIndex, SpriteEffects effects)
        {
            string frameName = $"adventurer_{frameIndex}";
            _tilesheetManager.DrawTile(spriteBatch, frameName, "fantasy", position, Color.White, 0f, Vector2.Zero, 1f, effects);
        }

        // Backward compatibility methods
        public Texture2D GetTexture(string name)
        {
            return _individualTextures.ContainsKey(name) ? _individualTextures[name] : null;
        }

        public Texture2D GetTerrainTexture(TerrainType terrainType)
        {
            return _tilesheetManager.GetSheet("fantasy"); // Returns the whole sheet - not ideal but compatible
        }

        public Texture2D GetObjectTexture(ObjectType objectType)
        {
            return _tilesheetManager.GetSheet("fantasy"); // Returns the whole sheet - not ideal but compatible
        }
    }
}