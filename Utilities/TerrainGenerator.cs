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
    private int _tileSize = 32; // 32x32 pixels per tile
    private AssetManager _assetManager;

    public TerrainGenerator(int seed = 0)
    {
        _loadedChunks = new Dictionary<Point, TerrainChunk>();
        _random = seed == 0 ? new Random() : new Random(seed);
    }

    public void LoadContent(AssetManager assetManager)
    {
        _assetManager = assetManager;
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
                
                TerrainType terrainType;
                if (height < 0.3f)
                    terrainType = TerrainType.Water;
                else if (height < 0.5f)
                    terrainType = TerrainType.Dirt;
                else if (height < 0.8f)
                    terrainType = TerrainType.Grass;
                else
                    terrainType = TerrainType.Stone;
                
                chunk.SetTile(x, y, terrainType);
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
            chunk.Draw(spriteBatch, _tileSize, _assetManager);
        }
    }

    public TerrainChunk GetChunk(Point chunkCoord)
    {
        return _loadedChunks.TryGetValue(chunkCoord, out var chunk) ? chunk : null;
    }
}

public class TerrainChunk
{
    public Point ChunkCoord { get; }
    private TerrainType[,] _tiles;
    private int _size;

    public TerrainChunk(Point chunkCoord, int size)
    {
        ChunkCoord = chunkCoord;
        _size = size;
        _tiles = new TerrainType[size, size];
    }

    public void SetTile(int x, int y, TerrainType terrainType)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size)
        {
            _tiles[x, y] = terrainType;
        }
    }

    public TerrainType GetTile(int x, int y)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size)
        {
            return _tiles[x, y];
        }
        return TerrainType.Grass; // Default to passable terrain if out of bounds
    }

    public void Draw(SpriteBatch spriteBatch, int tileSize, AssetManager assetManager)
    {
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                Vector2 position = new Vector2(
                    (ChunkCoord.X * _size + x) * tileSize,
                    (ChunkCoord.Y * _size + y) * tileSize
                );

                assetManager.DrawTerrain(spriteBatch, _tiles[x, y], position);
            }
        }
    }
}