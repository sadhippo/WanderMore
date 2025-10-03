using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class SimplePathfinder
{
    private const float TILE_SIZE = 32f;
    private const float OBSTACLE_PADDING = 16f; // Half a tile padding around obstacles
    private const int MAX_EDGE_FOLLOW_STEPS = 20; // Maximum steps to follow an obstacle edge
    
    public Queue<Vector2> FindPath(Vector2 start, Vector2 target, ZoneManager zoneManager, PoIManager poiManager)
    {
        var path = new Queue<Vector2>();
        float totalDistance = Vector2.Distance(start, target);
        
        // First, try direct line pathfinding
        if (IsDirectPathClear(start, target, zoneManager, poiManager))
        {
            path.Enqueue(target);
            System.Console.WriteLine($"[PATHFINDER] Direct path clear to target (distance: {totalDistance:F1})");
            return path;
        }
        
        // If direct path is blocked, use edge-following algorithm
        System.Console.WriteLine($"[PATHFINDER] Direct path blocked, finding route around obstacles (distance: {totalDistance:F1})");
        var edgePath = FindPathAroundObstacles(start, target, zoneManager, poiManager);
        foreach (var waypoint in edgePath)
        {
            path.Enqueue(waypoint);
        }
        
        System.Console.WriteLine($"[PATHFINDER] Generated path with {path.Count} waypoints");
        return path;
    }
    
    private bool IsDirectPathClear(Vector2 start, Vector2 target, ZoneManager zoneManager, PoIManager poiManager)
    {
        Vector2 direction = target - start;
        float distance = direction.Length();
        
        if (distance < 1f) return true; // Already at target
        
        direction.Normalize();
        
        // Check points along the path at regular intervals
        float stepSize = TILE_SIZE / 4f; // Check every 8 pixels
        int steps = (int)(distance / stepSize);
        
        for (int i = 1; i <= steps; i++)
        {
            Vector2 checkPoint = start + direction * (i * stepSize);
            if (!IsPositionWalkable(checkPoint, zoneManager, poiManager))
            {
                return false;
            }
        }
        
        // Final check at target position
        return IsPositionWalkable(target, zoneManager, poiManager);
    }
    
    private List<Vector2> FindPathAroundObstacles(Vector2 start, Vector2 target, ZoneManager zoneManager, PoIManager poiManager)
    {
        var waypoints = new List<Vector2>();
        
        // Find the first obstacle blocking our path
        Vector2 obstaclePoint = FindFirstObstacle(start, target, zoneManager, poiManager);
        if (obstaclePoint == Vector2.Zero)
        {
            // No obstacle found, direct path should work
            waypoints.Add(target);
            return waypoints;
        }
        
        // Try to navigate around the obstacle using edge-following
        var edgeWaypoints = FollowObstacleEdge(start, target, obstaclePoint, zoneManager, poiManager);
        waypoints.AddRange(edgeWaypoints);
        
        // Limit the number of waypoints to keep paths simple
        if (waypoints.Count > PathfindingConfig.MAX_PATH_LENGTH)
        {
            waypoints = waypoints.Take(PathfindingConfig.MAX_PATH_LENGTH).ToList();
        }
        
        return waypoints;
    }
    
    private Vector2 FindFirstObstacle(Vector2 start, Vector2 target, ZoneManager zoneManager, PoIManager poiManager)
    {
        Vector2 direction = target - start;
        float distance = direction.Length();
        direction.Normalize();
        
        float stepSize = TILE_SIZE / 2f; // Check every 16 pixels
        int steps = (int)(distance / stepSize);
        
        for (int i = 1; i <= steps; i++)
        {
            Vector2 checkPoint = start + direction * (i * stepSize);
            if (!IsPositionWalkable(checkPoint, zoneManager, poiManager))
            {
                return checkPoint;
            }
        }
        
        return Vector2.Zero; // No obstacle found
    }
    
    private List<Vector2> FollowObstacleEdge(Vector2 start, Vector2 target, Vector2 obstaclePoint, ZoneManager zoneManager, PoIManager poiManager)
    {
        var waypoints = new List<Vector2>();
        
        // Determine which direction to follow the obstacle edge
        Vector2 toTarget = target - start;
        Vector2 toObstacle = obstaclePoint - start;
        
        // Use cross product to determine if we should go clockwise or counterclockwise
        float cross = toTarget.X * toObstacle.Y - toTarget.Y * toObstacle.X;
        bool clockwise = cross > 0;
        
        // Start following the edge from a point near the obstacle
        Vector2 currentPos = FindWalkablePositionNear(obstaclePoint, zoneManager, poiManager);
        Vector2 lastDirection = Vector2.Normalize(toTarget);
        
        for (int step = 0; step < MAX_EDGE_FOLLOW_STEPS; step++)
        {
            // Try to move toward target if path is clear
            if (IsDirectPathClear(currentPos, target, zoneManager, poiManager))
            {
                waypoints.Add(target);
                break;
            }
            
            // Find next position along obstacle edge
            Vector2 nextPos = FindNextEdgePosition(currentPos, lastDirection, clockwise, zoneManager, poiManager);
            
            if (nextPos == currentPos)
            {
                // Stuck, try the other direction
                clockwise = !clockwise;
                nextPos = FindNextEdgePosition(currentPos, lastDirection, clockwise, zoneManager, poiManager);
                
                if (nextPos == currentPos)
                {
                    // Still stuck, abandon pathfinding
                    break;
                }
            }
            
            waypoints.Add(nextPos);
            lastDirection = Vector2.Normalize(nextPos - currentPos);
            currentPos = nextPos;
            
            // Check if we've made significant progress toward target
            float distanceToTarget = Vector2.Distance(currentPos, target);
            if (distanceToTarget < TILE_SIZE * 2f)
            {
                // Close enough to target, try direct path
                if (IsDirectPathClear(currentPos, target, zoneManager, poiManager))
                {
                    waypoints.Add(target);
                    break;
                }
            }
        }
        
        // If we didn't reach the target, add it as final waypoint anyway
        if (waypoints.Count == 0 || Vector2.Distance(waypoints.Last(), target) > TILE_SIZE)
        {
            waypoints.Add(target);
        }
        
        return waypoints;
    }
    
    private Vector2 FindNextEdgePosition(Vector2 currentPos, Vector2 lastDirection, bool clockwise, ZoneManager zoneManager, PoIManager poiManager)
    {
        // Try 8 directions around current position
        float[] angles = new float[8];
        for (int i = 0; i < 8; i++)
        {
            angles[i] = (i * MathHelper.PiOver4);
        }
        
        // Sort angles based on preference (continue in same general direction)
        float currentAngle = MathF.Atan2(lastDirection.Y, lastDirection.X);
        Array.Sort(angles, (a, b) => 
        {
            float diffA = Math.Abs(MathHelper.WrapAngle(a - currentAngle));
            float diffB = Math.Abs(MathHelper.WrapAngle(b - currentAngle));
            return diffA.CompareTo(diffB);
        });
        
        // If going clockwise, reverse the order
        if (clockwise)
        {
            Array.Reverse(angles);
        }
        
        // Try each direction
        foreach (float angle in angles)
        {
            Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 testPos = currentPos + direction * TILE_SIZE;
            
            if (IsPositionWalkable(testPos, zoneManager, poiManager))
            {
                return testPos;
            }
        }
        
        return currentPos; // No valid position found
    }
    
    public bool IsPositionWalkable(Vector2 position, ZoneManager zoneManager, PoIManager poiManager)
    {
        // Check zone bounds
        if (!zoneManager.IsPositionInBounds(position))
        {
            return false;
        }
        
        // Check terrain collision
        var terrainType = zoneManager.GetTerrainAt(position);
        if (terrainType == TerrainType.Stone || terrainType == TerrainType.Water)
        {
            return false;
        }
        
        // Check PoI collision (buildings and large objects)
        if (poiManager != null)
        {
            var nearbyPoIs = poiManager.GetNearbyPoIs(position, 64f, zoneManager.CurrentZone?.Id);
            
            foreach (var poi in nearbyPoIs)
            {
                if (IsCollidablePoI(poi.Type))
                {
                    // Add padding around buildings to make them easier to path around
                    float padding = OBSTACLE_PADDING; // 16 pixels
                    Rectangle positionBounds = new Rectangle((int)position.X, (int)position.Y, 32, 32);
                    Rectangle poiBounds = new Rectangle(
                        (int)(poi.Position.X - padding), 
                        (int)(poi.Position.Y - padding), 
                        (int)(32 + padding * 2), 
                        (int)(32 + padding * 2)
                    );
                    
                    if (positionBounds.Intersects(poiBounds))
                    {
                        System.Console.WriteLine($"[PATHFINDER] Position {position} blocked by {poi.Type} at {poi.Position} (with {padding}px padding)");
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
    
    public Vector2 FindWalkablePositionNear(Vector2 target, ZoneManager zoneManager, PoIManager poiManager)
    {
        // First check if target position is already walkable
        if (IsPositionWalkable(target, zoneManager, poiManager))
        {
            return target;
        }
        
        // Search in expanding circles for a walkable position
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int angle = 0; angle < 360; angle += 45)
            {
                float radians = MathHelper.ToRadians(angle);
                Vector2 offset = new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * (radius * TILE_SIZE);
                Vector2 testPos = target + offset;
                
                if (IsPositionWalkable(testPos, zoneManager, poiManager))
                {
                    return testPos;
                }
            }
        }
        
        // Fallback: return original target even if not walkable
        return target;
    }
    
    private bool IsCollidablePoI(PoIType type)
    {
        // Buildings and large structures should block pathfinding
        return type switch
        {
            PoIType.Farmhouse or PoIType.Inn or PoIType.Cottage or PoIType.Hut or
            PoIType.Castle or PoIType.Chapel or PoIType.Oracle or PoIType.SkullFortress or
            PoIType.HauntedHouse or PoIType.TreeHouse or PoIType.Mine => true,
            _ => false // NPCs and animals don't block pathfinding
        };
    }
}