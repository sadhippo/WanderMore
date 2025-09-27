using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class ObjectGenerator
{
    private Dictionary<Point, List<WorldObject>> _chunkObjects;
    private Random _random;
    private AssetManager _assetManager;

    public ObjectGenerator(int seed = 0)
    {
        _chunkObjects = new Dictionary<Point, List<WorldObject>>();
        _random = seed == 0 ? new Random() : new Random(seed);
    }

    public void LoadContent(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public void GenerateObjectsForChunk(Point chunkCoord, TerrainChunk terrainChunk, int chunkSize, int tileSize)
    {
        if (_chunkObjects.ContainsKey(chunkCoord))
            return; // Already generated

        var objects = new List<WorldObject>();

        // Generate objects based on terrain
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var terrainType = terrainChunk.GetTile(x, y);
                Vector2 worldPosition = new Vector2(
                    (chunkCoord.X * chunkSize + x) * tileSize,
                    (chunkCoord.Y * chunkSize + y) * tileSize
                );

                // Generate objects based on terrain type
                if (terrainType == TerrainType.Grass)
                {
                    GenerateGrasslandObjects(objects, worldPosition, x, y, chunkCoord);
                }
                else if (terrainType == TerrainType.Dirt)
                {
                    GenerateDirtObjects(objects, worldPosition, x, y, chunkCoord);
                }
            }
        }

        _chunkObjects[chunkCoord] = objects;
    }

    private void GenerateGrasslandObjects(List<WorldObject> objects, Vector2 position, int localX, int localY, Point chunkCoord)
    {
        // Use deterministic random based on world position
        int seed = (chunkCoord.X * 1000 + chunkCoord.Y) * 10000 + (localX * 100 + localY);
        var localRandom = new Random(seed);

        // Check for forest generation (clusters of double trees)
        if (ShouldGenerateForest(localX, localY, chunkCoord))
        {
            objects.Add(new WorldObject
            {
                Position = position,
                Type = ObjectType.DoubleTree
            });
        }
        // Single trees scattered on grass
        else if (localRandom.NextDouble() < 0.08) // 8% chance for single tree
        {
            objects.Add(new WorldObject
            {
                Position = position,
                Type = ObjectType.SingleTree
            });
        }
        // Occasional rocks
        else if (localRandom.NextDouble() < 0.02) // 2% chance for rocks
        {
            objects.Add(new WorldObject
            {
                Position = position,
                Type = ObjectType.Rock
            });
        }
    }

    private void GenerateDirtObjects(List<WorldObject> objects, Vector2 position, int localX, int localY, Point chunkCoord)
    {
        // Use deterministic random based on world position
        int seed = (chunkCoord.X * 1000 + chunkCoord.Y) * 10000 + (localX * 100 + localY);
        var localRandom = new Random(seed);

        // Occasional bushes and plants on dirt
        if (localRandom.NextDouble() < 0.05) // 5% chance for vegetation
        {
            var vegetationType = localRandom.NextDouble() < 0.6 ? ObjectType.Bush : ObjectType.Plant;
            objects.Add(new WorldObject
            {
                Position = position,
                Type = vegetationType
            });
        }
    }

    private bool ShouldGenerateForest(int localX, int localY, Point chunkCoord)
    {
        // Create forest clusters using simple noise-like function
        int worldX = chunkCoord.X * 32 + localX;
        int worldY = chunkCoord.Y * 32 + localY;

        // Create forest "hotspots" every ~20 tiles
        float forestNoise = 0f;
        forestNoise += MathF.Sin(worldX * 0.05f) * MathF.Sin(worldY * 0.05f);
        forestNoise += MathF.Sin(worldX * 0.1f + 100) * MathF.Sin(worldY * 0.1f + 100) * 0.5f;

        // Forest appears where noise is high
        return forestNoise > 0.7f;
    }

    public void Update(Vector2 playerPosition, TerrainGenerator terrainGenerator, int chunkSize, int tileSize)
    {
        // Calculate which chunk the player is in
        Point playerChunk = new Point(
            (int)Math.Floor(playerPosition.X / (chunkSize * tileSize)),
            (int)Math.Floor(playerPosition.Y / (chunkSize * tileSize))
        );

        // Generate objects for chunks around the player (3x3 grid)
        for (int x = playerChunk.X - 1; x <= playerChunk.X + 1; x++)
        {
            for (int y = playerChunk.Y - 1; y <= playerChunk.Y + 1; y++)
            {
                Point chunkCoord = new Point(x, y);
                if (!_chunkObjects.ContainsKey(chunkCoord))
                {
                    var terrainChunk = terrainGenerator.GetChunk(chunkCoord);
                    if (terrainChunk != null)
                    {
                        GenerateObjectsForChunk(chunkCoord, terrainChunk, chunkSize, tileSize);
                    }
                }
            }
        }

        // Unload distant object chunks
        var chunksToRemove = new List<Point>();
        foreach (var chunk in _chunkObjects.Keys)
        {
            float distance = Vector2.Distance(
                new Vector2(chunk.X, chunk.Y),
                new Vector2(playerChunk.X, playerChunk.Y)
            );
            if (distance > 3f)
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            _chunkObjects.Remove(chunk);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var chunkObjects in _chunkObjects.Values)
        {
            foreach (var obj in chunkObjects)
            {
                _assetManager.DrawObject(spriteBatch, obj.Type, obj.Position);
            }
        }
    }
}

public class WorldObject
{
    public Vector2 Position { get; set; }
    public ObjectType Type { get; set; }
}