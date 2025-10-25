using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class ZoneManager
{
    private Zone _currentZone;
    private Dictionary<string, Zone> _zones;
    private AssetManager _assetManager;
    private PoIManager _poiManager;
    private Random _random;
    private string _previousZoneId;

    public Zone CurrentZone => _currentZone;
    public bool ZoneChanged { get; private set; }

    public ZoneManager(int seed = 0)
    {
        _zones = new Dictionary<string, Zone>();
        _random = seed == 0 ? new Random() : new Random(seed);
        CreateStartingZone();
    }

    public void LoadContent(AssetManager assetManager, PoIManager poiManager)
    {
        try
        {
            _assetManager = assetManager;
            _poiManager = poiManager;
            
            // Generate terrain for the starting zone
            foreach (var zone in _zones.Values)
            {
                GenerateZoneTerrain(zone);
            }
            
            // Start in the first zone (should be the starting zone we created)
            _currentZone = _zones.Values.First();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error in ZoneManager.LoadContent: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void CreateStartingZone()
    {
        try
        {
            System.Console.WriteLine("Creating starting zone...");
            // Create the initial starting zone
            var startingZone = GenerateZone("start_0_0", BiomeType.Forest, 0, 0);
            _zones[startingZone.Id] = startingZone;
            System.Console.WriteLine($"Created starting zone: {startingZone.Id} ({startingZone.Width}x{startingZone.Height})");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error creating starting zone: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private Zone GenerateZone(string id, BiomeType biomeType, int worldX, int worldY)
    {
        try
        {
            // Generate zone size within reasonable bounds
            int width = _random.Next(40, 80);  // 40-80 tiles wide
            int height = _random.Next(40, 80); // 40-80 tiles tall
            
            var zone = new Zone
            {
                Id = id,
                Width = width,
                Height = height,
                BiomeType = biomeType,
                WorldX = worldX,
                WorldY = worldY,
                Connections = new Dictionary<Direction, string>(),
                GeneratedConnections = new Dictionary<Direction, bool>()
            };

            // Set name and description based on biome
            SetZoneDetails(zone);
            
            // Initialize connections as not generated
            zone.GeneratedConnections[Direction.North] = false;
            zone.GeneratedConnections[Direction.East] = false;
            zone.GeneratedConnections[Direction.South] = false;
            zone.GeneratedConnections[Direction.West] = false;

            System.Console.WriteLine($"Generated zone {id}: {zone.Name} ({width}x{height}) at world ({worldX},{worldY})");
            return zone;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error generating zone {id}: {ex.Message}");
            throw;
        }
    }

    private void SetZoneDetails(Zone zone)
    {
        var biomeNames = GetBiomeNames(zone.BiomeType);
        var randomName = biomeNames[_random.Next(biomeNames.Length)];
        
        zone.Name = randomName;
        zone.Description = GetBiomeDescription(zone.BiomeType);
    }

    private string[] GetBiomeNames(BiomeType biomeType)
    {
        return biomeType switch
        {
            BiomeType.Forest => new[] { "Emerald Grove", "Whispering Woods", "Verdant Meadow", "Ancient Forest", "Silverleaf Glade" },
            BiomeType.Lake => new[] { "Crystal Lake", "Moonlit Waters", "Serene Pool", "Mirror Lake", "Tranquil Bay" },
            BiomeType.Mountain => new[] { "Stone Peaks", "Granite Heights", "Rocky Highlands", "Windswept Crags", "Iron Cliffs" },
            BiomeType.DenseForest => new[] { "Shadowmere Woods", "Thornwood Thicket", "Darkleaf Forest", "Bramblewood", "Mistwood Grove" },
            BiomeType.Plains => new[] { "Golden Fields", "Windswept Plains", "Endless Meadows", "Sunlit Grasslands", "Rolling Hills" },
            BiomeType.Swamp => new[] { "Murky Marshes", "Foggy Wetlands", "Misty Bogs", "Shadowfen", "Gloomwater Swamp" },
            _ => new[] { "Unknown Lands" }
        };
    }

    private string GetBiomeDescription(BiomeType biomeType)
    {
        return biomeType switch
        {
            BiomeType.Forest => "A peaceful woodland with scattered trees and open meadows",
            BiomeType.Lake => "Pristine waters surrounded by gentle shores",
            BiomeType.Mountain => "Rocky terrain with stone formations and hardy vegetation",
            BiomeType.DenseForest => "A thick forest where sunlight barely penetrates the canopy",
            BiomeType.Plains => "Wide open grasslands stretching to the horizon",
            BiomeType.Swamp => "Murky wetlands filled with mysterious mists",
            _ => "A mysterious and unexplored region"
        };
    }

    private void GenerateZoneTerrain(Zone zone)
    {
        try
        {
            zone.Terrain = new TerrainType[zone.Width, zone.Height];
            zone.Objects = new List<WorldObject>();
            zone.ExploredTiles = new bool[zone.Width, zone.Height];

            System.Console.WriteLine($"Generating terrain for zone {zone.Id} ({zone.Width}x{zone.Height}) - {zone.BiomeType}");

            // Generate terrain based on biome type
            switch (zone.BiomeType)
            {
                case BiomeType.Forest:
                    GenerateForestTerrain(zone);
                    break;
                case BiomeType.Lake:
                    GenerateLakeTerrain(zone);
                    break;
                case BiomeType.Mountain:
                    GenerateMountainTerrain(zone);
                    break;
                case BiomeType.DenseForest:
                    GenerateDenseForestTerrain(zone);
                    break;
                case BiomeType.Plains:
                    GeneratePlainsTerrain(zone);
                    break;
                case BiomeType.Swamp:
                    GenerateSwampTerrain(zone);
                    break;
                default:
                    // Fallback to forest if unknown biome
                    GenerateForestTerrain(zone);
                    break;
            }

            System.Console.WriteLine($"Generated {zone.Objects.Count} objects for zone {zone.Id}");
            
            // Generate PoIs for this zone if PoIManager is available
            if (_poiManager != null)
            {
                _poiManager.GeneratePoIsForZone(zone, 32, 32);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error generating terrain for zone {zone.Id}: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void GenerateForestTerrain(Zone zone)
    {
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                float distanceFromCenter = Vector2.Distance(
                    new Vector2(x, y), 
                    new Vector2(zone.Width / 2f, zone.Height / 2f)
                ) / (zone.Width / 2f);

                // Create a meadow in the center, forest around edges
                if (distanceFromCenter < 0.3f)
                {
                    zone.Terrain[x, y] = TerrainType.Grass;
                    // Scattered single trees in meadow
                    if (_random.NextDouble() < 0.1)
                    {
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = ObjectType.SingleTree
                        });
                    }
                }
                else if (distanceFromCenter < 0.7f)
                {
                    zone.Terrain[x, y] = _random.NextDouble() < 0.7 ? TerrainType.Grass : TerrainType.Dirt;
                    // More trees as we get to forest edge
                    if (_random.NextDouble() < 0.25)
                    {
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = _random.NextDouble() < 0.6 ? ObjectType.SingleTree : ObjectType.DoubleTree
                        });
                    }
                }
                else
                {
                    zone.Terrain[x, y] = TerrainType.Dirt;
                    // Dense forest at edges
                    if (_random.NextDouble() < 0.4)
                    {
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = ObjectType.DoubleTree
                        });
                    }
                }
            }
        }
    }

    private void GenerateLakeTerrain(Zone zone)
    {
        Vector2 center = new Vector2(zone.Width / 2f, zone.Height / 2f);
        
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), center) / (zone.Width / 3f);

                if (distanceFromCenter < 0.6f)
                {
                    zone.Terrain[x, y] = TerrainType.Water;
                }
                else if (distanceFromCenter < 0.8f)
                {
                    zone.Terrain[x, y] = TerrainType.Dirt; // Shore
                }
                else
                {
                    zone.Terrain[x, y] = TerrainType.Grass;
                    // Trees around the lake
                    if (_random.NextDouble() < 0.15)
                    {
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = ObjectType.SingleTree
                        });
                    }
                }
            }
        }
    }

    private void GenerateMountainTerrain(Zone zone)
    {
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                // Create height map using noise
                float height = GetNoiseValue(x, y, zone.Width);
                
                if (height > 0.7f)
                {
                    zone.Terrain[x, y] = TerrainType.Stone;
                }
                else if (height > 0.4f)
                {
                    zone.Terrain[x, y] = TerrainType.Dirt;
                    // Occasional rocks
                    if (_random.NextDouble() < 0.08)
                    {
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = ObjectType.Rock
                        });
                    }
                }
                else
                {
                    zone.Terrain[x, y] = TerrainType.Grass;
                }
            }
        }
    }

    private void GenerateDenseForestTerrain(Zone zone)
    {
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                zone.Terrain[x, y] = _random.NextDouble() < 0.8 ? TerrainType.Grass : TerrainType.Dirt;
                
                // Very dense tree coverage
                if (_random.NextDouble() < 0.5)
                {
                    zone.Objects.Add(new WorldObject
                    {
                        Position = new Vector2(x * 32, y * 32),
                        Type = _random.NextDouble() < 0.7 ? ObjectType.DoubleTree : ObjectType.SingleTree
                    });
                }
                else if (_random.NextDouble() < 0.1)
                {
                    zone.Objects.Add(new WorldObject
                    {
                        Position = new Vector2(x * 32, y * 32),
                        Type = _random.NextDouble() < 0.5 ? ObjectType.Bush : ObjectType.Plant
                    });
                }
            }
        }
    }

    private float GetNoiseValue(int x, int y, int scale)
    {
        float value = 0f;
        value += MathF.Sin(x * 0.1f) * 0.5f;
        value += MathF.Sin(y * 0.1f) * 0.5f;
        value += MathF.Sin((x + y) * 0.05f) * 0.3f;
        return (value + 1.3f) / 2.6f;
    }

    private void GeneratePlainsTerrain(Zone zone)
    {
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                // Mostly grass with occasional dirt patches
                float noise = GetNoiseValue(x, y, zone.Width);
                
                if (noise < 0.2f)
                {
                    zone.Terrain[x, y] = TerrainType.Dirt;
                }
                else
                {
                    zone.Terrain[x, y] = TerrainType.Grass;
                }
                
                // Very sparse trees and rocks
                if (_random.NextDouble() < 0.03)
                {
                    zone.Objects.Add(new WorldObject
                    {
                        Position = new Vector2(x * 32, y * 32),
                        Type = ObjectType.SingleTree
                    });
                }
                else if (_random.NextDouble() < 0.02)
                {
                    zone.Objects.Add(new WorldObject
                    {
                        Position = new Vector2(x * 32, y * 32),
                        Type = ObjectType.Rock
                    });
                }
            }
        }
    }

    private void GenerateSwampTerrain(Zone zone)
    {
        for (int x = 0; x < zone.Width; x++)
        {
            for (int y = 0; y < zone.Height; y++)
            {
                float noise = GetNoiseValue(x, y, zone.Width);
                float waterNoise = GetNoiseValue(x + 100, y + 100, zone.Width);
                
                // Mix of water, dirt, and some grass
                if (waterNoise < 0.4f)
                {
                    zone.Terrain[x, y] = TerrainType.Water;
                }
                else if (noise < 0.6f)
                {
                    zone.Terrain[x, y] = TerrainType.Dirt;
                }
                else
                {
                    zone.Terrain[x, y] = TerrainType.Grass;
                }
                
                // Swamp vegetation - lots of bushes and plants
                if (zone.Terrain[x, y] != TerrainType.Water)
                {
                    if (_random.NextDouble() < 0.15)
                    {
                        var vegetationType = _random.NextDouble() < 0.7 ? ObjectType.Bush : ObjectType.Plant;
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = vegetationType
                        });
                    }
                    else if (_random.NextDouble() < 0.08)
                    {
                        zone.Objects.Add(new WorldObject
                        {
                            Position = new Vector2(x * 32, y * 32),
                            Type = ObjectType.SingleTree
                        });
                    }
                }
            }
        }
    }

    public TerrainType GetTerrainAt(Vector2 worldPosition)
    {
        int tileX = (int)(worldPosition.X / 32);
        int tileY = (int)(worldPosition.Y / 32);
        
        if (tileX >= 0 && tileX < _currentZone.Width && tileY >= 0 && tileY < _currentZone.Height)
        {
            return _currentZone.Terrain[tileX, tileY];
        }
        
        // Outside zone boundaries - this will be treated as solid for collision
        return TerrainType.Stone; // Treat boundaries as solid
    }

    public bool IsPositionInBounds(Vector2 worldPosition)
    {
        int tileX = (int)(worldPosition.X / 32);
        int tileY = (int)(worldPosition.Y / 32);
        
        return tileX >= 0 && tileX < _currentZone.Width && tileY >= 0 && tileY < _currentZone.Height;
    }

    public bool TryTransitionZone(Vector2 worldPosition, out Vector2 newPosition)
    {
        newPosition = worldPosition;
        
        int tileX = (int)(worldPosition.X / 32);
        int tileY = (int)(worldPosition.Y / 32);
        
        Direction? transitionDirection = null;
        
        // Check which boundary was crossed
        if (tileX < 0)
            transitionDirection = Direction.West;
        else if (tileX >= _currentZone.Width)
            transitionDirection = Direction.East;
        else if (tileY < 0)
            transitionDirection = Direction.North;
        else if (tileY >= _currentZone.Height)
            transitionDirection = Direction.South;
        
        if (transitionDirection.HasValue)
        {
            // Check if we already have a connection in this direction
            if (_currentZone.Connections.TryGetValue(transitionDirection.Value, out string existingZoneId) && 
                existingZoneId != null)
            {
                // Use existing connection
                TransitionToZone(existingZoneId, transitionDirection.Value, worldPosition, out newPosition);
                return true;
            }
            else if (!_currentZone.GeneratedConnections[transitionDirection.Value])
            {
                // Generate a new zone in this direction
                var newZone = GenerateConnectedZone(_currentZone, transitionDirection.Value);
                if (newZone != null)
                {
                    _zones[newZone.Id] = newZone;
                    GenerateZoneTerrain(newZone);
                    
                    // Create bidirectional connection
                    _currentZone.Connections[transitionDirection.Value] = newZone.Id;
                    _currentZone.GeneratedConnections[transitionDirection.Value] = true;
                    
                    Direction oppositeDirection = GetOppositeDirection(transitionDirection.Value);
                    newZone.Connections[oppositeDirection] = _currentZone.Id;
                    newZone.GeneratedConnections[oppositeDirection] = true;
                    
                    TransitionToZone(newZone.Id, transitionDirection.Value, worldPosition, out newPosition);
                    return true;
                }
            }
        }
        
        return false; // No valid transition
    }

    private void TransitionToZone(string zoneId, Direction direction, Vector2 worldPosition, out Vector2 newPosition)
    {
        _previousZoneId = _currentZone.Id;
        var nextZone = _zones[zoneId];
        _currentZone = nextZone;
        ZoneChanged = true;
        
        // Calculate safe spawn position in the target zone with padding
        int padding = 3; // 3 tiles from edge
        
        switch (direction)
        {
            case Direction.North:
                // Coming from south, spawn near bottom with padding
                newPosition = FindSafeSpawnPosition(nextZone, 
                    (int)(worldPosition.X / 32), 
                    nextZone.Height - padding - 1, 
                    direction);
                break;
            case Direction.South:
                // Coming from north, spawn near top with padding
                newPosition = FindSafeSpawnPosition(nextZone, 
                    (int)(worldPosition.X / 32), 
                    padding, 
                    direction);
                break;
            case Direction.East:
                // Coming from west, spawn near left with padding
                newPosition = FindSafeSpawnPosition(nextZone, 
                    padding, 
                    (int)(worldPosition.Y / 32), 
                    direction);
                break;
            case Direction.West:
                // Coming from east, spawn near right with padding
                newPosition = FindSafeSpawnPosition(nextZone, 
                    nextZone.Width - padding - 1, 
                    (int)(worldPosition.Y / 32), 
                    direction);
                break;
            default:
                newPosition = FindSafeSpawnPosition(nextZone, nextZone.Width / 2, nextZone.Height / 2, direction);
                break;
        }
    }

    private Vector2 FindSafeSpawnPosition(Zone zone, int preferredX, int preferredY, Direction fromDirection)
    {
        // Clamp to zone bounds with padding
        int padding = 3;
        preferredX = Math.Clamp(preferredX, padding, zone.Width - padding - 1);
        preferredY = Math.Clamp(preferredY, padding, zone.Height - padding - 1);
        
        // Check if preferred position is safe (not water or stone)
        if (IsTileSafe(zone, preferredX, preferredY))
        {
            return new Vector2(preferredX * 32, preferredY * 32);
        }
        
        // Search in expanding circles for a safe position
        for (int radius = 1; radius <= 10; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) == radius || Math.Abs(dy) == radius) // Only check perimeter
                    {
                        int checkX = preferredX + dx;
                        int checkY = preferredY + dy;
                        
                        if (checkX >= padding && checkX < zone.Width - padding &&
                            checkY >= padding && checkY < zone.Height - padding &&
                            IsTileSafe(zone, checkX, checkY))
                        {
                            return new Vector2(checkX * 32, checkY * 32);
                        }
                    }
                }
            }
        }
        
        // Fallback: find any safe position in the zone
        for (int x = padding; x < zone.Width - padding; x++)
        {
            for (int y = padding; y < zone.Height - padding; y++)
            {
                if (IsTileSafe(zone, x, y))
                {
                    return new Vector2(x * 32, y * 32);
                }
            }
        }
        
        // Last resort: center of zone (even if not ideal)
        return new Vector2((zone.Width / 2) * 32, (zone.Height / 2) * 32);
    }

    private bool IsTileSafe(Zone zone, int x, int y)
    {
        if (x < 0 || x >= zone.Width || y < 0 || y >= zone.Height)
            return false;
            
        var terrainType = zone.Terrain[x, y];
        
        // Safe terrain: grass and dirt
        // Unsafe terrain: water and stone
        return terrainType == TerrainType.Grass || terrainType == TerrainType.Dirt;
    }

    private Zone GenerateConnectedZone(Zone currentZone, Direction direction)
    {
        // Calculate world coordinates for the new zone
        int newWorldX = currentZone.WorldX;
        int newWorldY = currentZone.WorldY;
        
        switch (direction)
        {
            case Direction.North: newWorldY--; break;
            case Direction.South: newWorldY++; break;
            case Direction.East: newWorldX++; break;
            case Direction.West: newWorldX--; break;
        }
        
        // Generate appropriate biome based on current biome and transition rules
        BiomeType newBiome = SelectNextBiome(currentZone.BiomeType, direction);
        
        string zoneId = $"zone_{newWorldX}_{newWorldY}";
        return GenerateZone(zoneId, newBiome, newWorldX, newWorldY);
    }

    private BiomeType SelectNextBiome(BiomeType currentBiome, Direction direction)
    {
        // Define biome transition probabilities for logical world building
        var transitions = GetBiomeTransitions(currentBiome);
        
        // Add some directional bias (e.g., mountains more likely to the north)
        var biasedTransitions = ApplyDirectionalBias(transitions, direction);
        
        // Select random biome based on weighted probabilities
        float totalWeight = 0f;
        foreach (var weight in biasedTransitions.Values)
            totalWeight += weight;
        
        float randomValue = (float)_random.NextDouble() * totalWeight;
        float currentWeight = 0f;
        
        foreach (var kvp in biasedTransitions)
        {
            currentWeight += kvp.Value;
            if (randomValue <= currentWeight)
                return kvp.Key;
        }
        
        return currentBiome; // Fallback
    }

    private Dictionary<BiomeType, float> GetBiomeTransitions(BiomeType currentBiome)
    {
        return currentBiome switch
        {
            BiomeType.Forest => new Dictionary<BiomeType, float>
            {
                { BiomeType.Forest, 0.3f },
                { BiomeType.DenseForest, 0.25f },
                { BiomeType.Plains, 0.2f },
                { BiomeType.Lake, 0.15f },
                { BiomeType.Mountain, 0.1f }
            },
            BiomeType.DenseForest => new Dictionary<BiomeType, float>
            {
                { BiomeType.DenseForest, 0.4f },
                { BiomeType.Forest, 0.3f },
                { BiomeType.Swamp, 0.2f },
                { BiomeType.Lake, 0.1f }
            },
            BiomeType.Plains => new Dictionary<BiomeType, float>
            {
                { BiomeType.Plains, 0.35f },
                { BiomeType.Forest, 0.25f },
                { BiomeType.Lake, 0.2f },
                { BiomeType.Mountain, 0.2f }
            },
            BiomeType.Lake => new Dictionary<BiomeType, float>
            {
                { BiomeType.Forest, 0.3f },
                { BiomeType.Plains, 0.25f },
                { BiomeType.Swamp, 0.2f },
                { BiomeType.Lake, 0.15f },
                { BiomeType.Mountain, 0.1f }
            },
            BiomeType.Mountain => new Dictionary<BiomeType, float>
            {
                { BiomeType.Mountain, 0.4f },
                { BiomeType.Plains, 0.25f },
                { BiomeType.Forest, 0.2f },
                { BiomeType.Lake, 0.15f }
            },
            BiomeType.Swamp => new Dictionary<BiomeType, float>
            {
                { BiomeType.Swamp, 0.35f },
                { BiomeType.DenseForest, 0.25f },
                { BiomeType.Lake, 0.25f },
                { BiomeType.Forest, 0.15f }
            },
            _ => new Dictionary<BiomeType, float> { { BiomeType.Forest, 1.0f } }
        };
    }

    private Dictionary<BiomeType, float> ApplyDirectionalBias(Dictionary<BiomeType, float> transitions, Direction direction)
    {
        var biased = new Dictionary<BiomeType, float>(transitions);
        
        // Apply directional biases for more realistic world generation
        switch (direction)
        {
            case Direction.North:
                // Mountains and harsh terrain more likely to the north
                if (biased.ContainsKey(BiomeType.Mountain))
                    biased[BiomeType.Mountain] *= 1.5f;
                break;
                
            case Direction.South:
                // Warmer, more lush biomes to the south
                if (biased.ContainsKey(BiomeType.Swamp))
                    biased[BiomeType.Swamp] *= 1.3f;
                if (biased.ContainsKey(BiomeType.DenseForest))
                    biased[BiomeType.DenseForest] *= 1.2f;
                break;
                
            case Direction.East:
                // Lakes and water features more common to the east
                if (biased.ContainsKey(BiomeType.Lake))
                    biased[BiomeType.Lake] *= 1.4f;
                break;
                
            case Direction.West:
                // Plains and open areas more common to the west
                if (biased.ContainsKey(BiomeType.Plains))
                    biased[BiomeType.Plains] *= 1.3f;
                break;
        }
        
        return biased;
    }

    private Direction GetOppositeDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => direction
        };
    }

    public void MarkTileExplored(Vector2 worldPosition)
    {
        int tileX = (int)(worldPosition.X / 32);
        int tileY = (int)(worldPosition.Y / 32);
        
        if (tileX >= 0 && tileX < _currentZone.Width && tileY >= 0 && tileY < _currentZone.Height)
        {
            _currentZone.ExploredTiles[tileX, tileY] = true;
        }
    }

    public void Update(Vector2 playerPosition)
    {
        // Reset zone changed flag
        ZoneChanged = false;
        
        // Mark tiles around player as explored
        int tileX = (int)(playerPosition.X / 32);
        int tileY = (int)(playerPosition.Y / 32);
        
        // Mark a 3x3 area around player as explored
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int checkX = tileX + dx;
                int checkY = tileY + dy;
                
                if (checkX >= 0 && checkX < _currentZone.Width && 
                    checkY >= 0 && checkY < _currentZone.Height)
                {
                    _currentZone.ExploredTiles[checkX, checkY] = true;
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        DrawTerrain(spriteBatch);
        DrawObjects(spriteBatch);
    }

    public void DrawTerrain(SpriteBatch spriteBatch)
    {
        // Draw terrain (ground tiles)
        for (int x = 0; x < _currentZone.Width; x++)
        {
            for (int y = 0; y < _currentZone.Height; y++)
            {
                Vector2 position = new Vector2(x * 32, y * 32);
                _assetManager.DrawTerrain(spriteBatch, _currentZone.Terrain[x, y], position);
            }
        }
    }

    public void DrawObjects(SpriteBatch spriteBatch)
    {
        // Draw objects (trees, rocks, etc.)
        foreach (var obj in _currentZone.Objects)
        {
            _assetManager.DrawObject(spriteBatch, obj.Type, obj.Position);
        }
    }
}

public class Zone
{
    public string Name { get; set; }
    public string Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public BiomeType BiomeType { get; set; }
    public string Description { get; set; }
    public TerrainType[,] Terrain { get; set; }
    public List<WorldObject> Objects { get; set; }
    public bool[,] ExploredTiles { get; set; }
    public Dictionary<Direction, string> Connections { get; set; }
    public Dictionary<Direction, bool> GeneratedConnections { get; set; }
    public int WorldX { get; set; }
    public int WorldY { get; set; }
}

public enum BiomeType
{
    Forest,
    Lake,
    Mountain,
    DenseForest,
    Plains,
    Swamp
}

public enum Direction
{
    North,
    East,
    South,
    West
}