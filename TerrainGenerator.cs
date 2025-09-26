using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class TerrainGenerator
{
    private Dictionary<Point, TerrainChunk> _loadedChunks;
    private Random _random;
    private int _chunkSize = 32; // 32x32 tiles per chunk
    private int _tileSize = 16; // 16x16 pixels per tile
    private Texture2D _grassTexture;
    private Texture2D _stoneTexture;
    private Texture2D _waterTexture;

    public TerrainGenerator(int seed = 0)
    {
        _loadedChunks = new Dictionary<Point, TerrainChunk>();
        _random = seed == 0 ? new Random() : new Random(seed);
    }

    public void LoadContent(Texture2D grassTexture, Texture2D stoneTexture, Texture2D waterTexture)
    {
        _grassTexture = grassTexture;
        _stoneTexture = stoneTexture;
        _waterTexture = waterTexture;
    }

    public void Update(Vector2 playerPosition)
    {
        // Calculate which chunk the player is in
        Point playerChunk = new Point(
            (int)Math.Floor(playerPosition.X / (_chunkSize * _tileSize)),
            (int)Math.Floor(playerPosition.Y / (_chunkSize * _tileSize))
        );

        // Load chunks around the player (3x3 grid)
        for (int x = playerChunk.X - 1; x <= playerChunk.X + 1; x++)
        {
            for (int y = playerChunk.Y - 1; y <= playerChunk.Y + 1; y++)
            {
                Point chunkCoord = new Point(x, y);
                if (!_loadedChunks.ContainsKey(chunkCoord))
                {
                    _loadedChunks[chunkCoord] = GenerateChunk(chunkCoord);
                }
            }
        }

        // Unload distant chunks (simple distance check)
        var chunksToRemove = new List<Point>();
        foreach (var chunk in _loadedChunks.Keys)
        {
            float distance = Vector2.Distance(
                new Vector2(chunk.X, chunk.Y),
                new Vector2(playerChunk.X, playerChunk.Y)
            );
            if (distance > 3f) // Keep chunks within 3 chunk radius
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            _loadedChunks.Remove(chunk);
        }
    }

    private TerrainChunk GenerateChunk(Point chunkCoord)
    {
        var chunk = new TerrainChunk(chunkCoord, _chunkSize);
        
        // Simple procedural generation using noise-like patterns
        for (int x = 0; x < _chunkSize; x++)
        {
            for (int y = 0; y < _chunkSize; y++)
            {
                // World coordinates
                int worldX = chunkCoord.X * _chunkSize + x;
                int worldY = chunkCoord.Y * _chunkSize + y;
                
                // Simple height-based terrain generation
                float height = GetHeight(worldX, worldY);
                
                TileType tileType;
                if (height < 0.3f)
                    tileType = TileType.Water;
                else if (height < 0.7f)
                    tileType = TileType.Grass;
                else
                    tileType = TileType.Stone;
                
                chunk.SetTile(x, y, tileType);
            }
        }
        
        return chunk;
    }

    private float GetHeight(int x, int y)
    {
        // Simple pseudo-noise function
        float value = 0f;
        value += MathF.Sin(x * 0.1f) * 0.5f;
        value += MathF.Sin(y * 0.1f) * 0.5f;
        value += MathF.Sin((x + y) * 0.05f) * 0.3f;
        
        // Normalize to 0-1 range
        return (value + 1.3f) / 2.6f;
    }

    public void Draw(SpriteBatch spriteBatch, Camera camera)
    {
        foreach (var chunk in _loadedChunks.Values)
        {
            chunk.Draw(spriteBatch, _tileSize, _grassTexture, _stoneTexture, _waterTexture);
        }
    }
}

public enum TileType
{
    Grass,
    Stone,
    Water
}

public class TerrainChunk
{
    public Point ChunkCoord { get; }
    private TileType[,] _tiles;
    private int _size;

    public TerrainChunk(Point chunkCoord, int size)
    {
        ChunkCoord = chunkCoord;
        _size = size;
        _tiles = new TileType[size, size];
    }

    public void SetTile(int x, int y, TileType tileType)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size)
        {
            _tiles[x, y] = tileType;
        }
    }

    public TileType GetTile(int x, int y)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size)
        {
            return _tiles[x, y];
        }
        return TileType.Grass; // Default
    }

    public void Draw(SpriteBatch spriteBatch, int tileSize, Texture2D grassTexture, Texture2D stoneTexture, Texture2D waterTexture)
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                Vector2 position = new Vector2(
                    (ChunkCoord.X * _size + x) * tileSize,
                    (ChunkCoord.Y * _size + y) * tileSize
                );

                Texture2D texture = _tiles[x, y] switch
                {
                    TileType.Grass => grassTexture,
                    TileType.Stone => stoneTexture,
                    TileType.Water => waterTexture,
                    _ => grassTexture
                };

                if (texture != null)
                {
                    spriteBatch.Draw(texture, position, Color.White);
                }
            }
        }
    }
}