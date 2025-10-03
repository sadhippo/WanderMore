using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class PathfindingManager
{
    public enum PathfindingState { Wandering, Pathfinding, Moving }
    public enum TargetPriority { Wandering, Discovered, Quest } // Quest = highest priority
    
    public PathfindingState CurrentState { get; private set; }
    public PointOfInterest CurrentTarget { get; private set; }
    public TargetPriority CurrentPriority { get; private set; }
    public Queue<Vector2> CurrentPath { get; private set; }
    
    private PoIDetector _poiDetector;
    private SimplePathfinder _pathfinder;
    private float _detectionTimer;
    private float _pathfindingTimer;
    private Vector2 _lastPosition;
    private float _stuckTimer;
    private int _pathRecalculationAttempts;
    private const float DETECTION_INTERVAL = 0.2f; // Check for PoIs every 0.2 seconds (more responsive)
    private const int MAX_RECALCULATION_ATTEMPTS = 3; // Maximum times to recalculate before abandoning
    
    public PathfindingManager()
    {
        CurrentState = PathfindingState.Wandering;
        CurrentPriority = TargetPriority.Wandering;
        CurrentPath = new Queue<Vector2>();
        _poiDetector = new PoIDetector();
        _pathfinder = new SimplePathfinder();
        _detectionTimer = 0f;
        _pathfindingTimer = 0f;
        _stuckTimer = 0f;
        _pathRecalculationAttempts = 0;
        _lastPosition = Vector2.Zero;
        System.Console.WriteLine("[PATHFINDING] PathfindingManager created successfully");
    }
    
    public void Update(Vector2 currentPosition, ZoneManager zoneManager, PoIManager poiManager, float deltaTime, QuestManager questManager = null)
    {
        _detectionTimer += deltaTime;
        
        switch (CurrentState)
        {
            case PathfindingState.Wandering:
                UpdateWandering(currentPosition, poiManager, questManager);
                break;
                
            case PathfindingState.Pathfinding:
                UpdatePathfinding(currentPosition, zoneManager, poiManager);
                break;
                
            case PathfindingState.Moving:
                UpdateMoving(currentPosition, deltaTime);
                break;
        }
        
        // Debug logging every 5 seconds when pathfinding is active
        if (HasActiveTarget && _detectionTimer % 5.0f < deltaTime)
        {
            System.Console.WriteLine($"[PATHFINDING] Status: {GetPathfindingStatus()}");
        }
    }
    
    private void UpdateWandering(Vector2 currentPosition, PoIManager poiManager, QuestManager questManager = null)
    {
        // Periodically check for nearby PoIs
        if (_detectionTimer >= DETECTION_INTERVAL)
        {
            // First check for quest target PoIs (highest priority)
            if (questManager != null)
            {
                var questTargetPoI = GetQuestTargetPoI(questManager, poiManager, currentPosition);
                if (questTargetPoI != null)
                {
                    SetQuestTarget(questTargetPoI);
                    _detectionTimer = 0f;
                    return;
                }
            }
            
            // Then check for regular nearby PoIs
            var nearbyPoI = _poiDetector.FindNearestInterestingPoI(currentPosition, poiManager);
            if (nearbyPoI != null)
            {
                SetDiscoveredTarget(nearbyPoI);
                System.Console.WriteLine($"[PATHFINDING] Setting discovered target: {nearbyPoI.Type}");
            }
            _detectionTimer = 0f;
        }
    }
    
    private void UpdatePathfinding(Vector2 currentPosition, ZoneManager zoneManager, PoIManager poiManager)
    {
        // Check if target is still valid
        if (CurrentTarget == null || _poiDetector.IsPoIOnCooldown(CurrentTarget))
        {
            if (CurrentTarget != null && _poiDetector.IsPoIOnCooldown(CurrentTarget))
            {
                System.Console.WriteLine($"[PATHFINDING] Target {CurrentTarget.Type} is on cooldown, abandoning");
            }
            AbandonCurrentTarget();
            return;
        }
        
        // Calculate path to target using SimplePathfinder
        try
        {
            CurrentPath = _pathfinder.FindPath(currentPosition, CurrentTarget.Position, zoneManager, poiManager);
            
            if (CurrentPath.Count > 0)
            {
                CurrentState = PathfindingState.Moving;
                _pathfindingTimer = 0f;
                _pathRecalculationAttempts = 0; // Reset attempts on successful pathfinding
                System.Console.WriteLine($"[PATHFINDING] Found path with {CurrentPath.Count} waypoints to {CurrentTarget.Type} at distance {Vector2.Distance(currentPosition, CurrentTarget.Position):F1}");
            }
            else
            {
                // No path found, abandon target
                System.Console.WriteLine($"[PATHFINDING] No path found to {CurrentTarget.Type}, abandoning target");
                AbandonCurrentTarget();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Pathfinding error: {ex.Message}");
            AbandonCurrentTarget();
        }
    }
    
    private void UpdateMoving(Vector2 currentPosition, float deltaTime)
    {
        _pathfindingTimer += deltaTime;
        
        // Check for timeout
        if (_pathfindingTimer > PathfindingConfig.PATH_TIMEOUT)
        {
            System.Console.WriteLine("Pathfinding: Timeout reached, abandoning target");
            AbandonCurrentTarget();
            return;
        }
        
        // Check if we've reached the target
        if (CurrentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(currentPosition, CurrentTarget.Position);
            if (distanceToTarget <= CurrentTarget.InteractionRange)
            {
                // Reached target - trigger proper interaction instead of just adding cooldown
                System.Console.WriteLine($"[PATHFINDING] SUCCESS: Reached {CurrentPriority} target {CurrentTarget.Type} (distance: {distanceToTarget:F1}) - Starting interaction");
                
                // Set a flag or trigger interaction - the Adventurer class should handle the actual interaction
                // Don't abandon target immediately - let the interaction system handle it
                CurrentState = PathfindingState.Wandering; // Stop pathfinding but keep target for interaction
                return;
            }
        }
        
        // Check if we're making progress (stuck detection)
        float distanceMoved = Vector2.Distance(currentPosition, _lastPosition);
        if (distanceMoved < PathfindingConfig.MINIMUM_PROGRESS_DISTANCE)
        {
            _stuckTimer += deltaTime;
            if (_stuckTimer > PathfindingConfig.STUCK_DETECTION_TIME)
            {
                _pathRecalculationAttempts++;
                System.Console.WriteLine($"Pathfinding: Stuck detected (attempt {_pathRecalculationAttempts}/{MAX_RECALCULATION_ATTEMPTS}), recalculating path");
                
                if (_pathRecalculationAttempts >= MAX_RECALCULATION_ATTEMPTS)
                {
                    System.Console.WriteLine($"[PATHFINDING] FAILED: Maximum recalculation attempts reached for {CurrentTarget.Type}, abandoning");
                    AbandonCurrentTarget();
                    return;
                }
                
                // Try to recalculate path
                CurrentState = PathfindingState.Pathfinding;
                _stuckTimer = 0f;
                return;
            }
        }
        else
        {
            _stuckTimer = 0f; // Reset stuck timer if making progress
        }
        
        // Check if we need to recalculate path (deviated too far from current waypoint)
        if (CurrentPath.Count > 0)
        {
            Vector2 currentWaypoint = CurrentPath.Peek();
            float distanceToWaypoint = Vector2.Distance(currentPosition, currentWaypoint);
            
            if (distanceToWaypoint > PathfindingConfig.PATH_RECALC_DISTANCE)
            {
                _pathRecalculationAttempts++;
                System.Console.WriteLine($"Pathfinding: Deviated from path (attempt {_pathRecalculationAttempts}/{MAX_RECALCULATION_ATTEMPTS}), recalculating");
                
                if (_pathRecalculationAttempts >= MAX_RECALCULATION_ATTEMPTS)
                {
                    System.Console.WriteLine("Pathfinding: Maximum recalculation attempts reached, abandoning target");
                    AbandonCurrentTarget();
                    return;
                }
                
                CurrentState = PathfindingState.Pathfinding;
                return;
            }
            
            // Check if we've reached the current waypoint
            if (distanceToWaypoint < 32f) // TILE_SIZE equivalent
            {
                CurrentPath.Dequeue(); // Remove reached waypoint
                System.Console.WriteLine($"Pathfinding: Reached waypoint, {CurrentPath.Count} remaining");
            }
        }
        
        _lastPosition = currentPosition;
    }
    
    public Vector2 GetNextDirection(Vector2 currentPosition, Vector2 currentDirection)
    {
        if (CurrentState == PathfindingState.Moving && CurrentPath.Count > 0)
        {
            // Move toward the next waypoint in the path
            Vector2 nextWaypoint = CurrentPath.Peek();
            Vector2 directionToWaypoint = nextWaypoint - currentPosition;
            
            if (directionToWaypoint.Length() > 0)
            {
                directionToWaypoint.Normalize();
                return directionToWaypoint;
            }
        }
        else if (CurrentState == PathfindingState.Moving && CurrentTarget != null)
        {
            // Fallback: direct movement toward target if no waypoints
            Vector2 directionToTarget = CurrentTarget.Position - currentPosition;
            if (directionToTarget.Length() > 0)
            {
                directionToTarget.Normalize();
                return directionToTarget;
            }
        }
        
        // Return current direction for wandering mode
        return currentDirection;
    }
    
    public void SetQuestTarget(PointOfInterest target)
    {
        if (target == null) return;
        
        // Quest targets have highest priority
        if (CurrentPriority <= TargetPriority.Quest)
        {
            CurrentTarget = target;
            CurrentPriority = TargetPriority.Quest;
            CurrentState = PathfindingState.Pathfinding;
            CurrentPath.Clear();
            
            System.Console.WriteLine($"Pathfinding: Set quest target {target.Type} at {target.Position}");
        }
    }
    
    public void SetDiscoveredTarget(PointOfInterest target)
    {
        if (target == null) return;
        
        // Only set if we don't have a higher priority target
        if (CurrentPriority <= TargetPriority.Discovered)
        {
            CurrentTarget = target;
            CurrentPriority = TargetPriority.Discovered;
            CurrentState = PathfindingState.Pathfinding;
            CurrentPath.Clear();
            
            System.Console.WriteLine($"Pathfinding: Discovered target {target.Type} at {target.Position}");
        }
    }
    
    public void AbandonCurrentTarget()
    {
        if (CurrentTarget != null)
        {
            System.Console.WriteLine($"Pathfinding: Abandoning target {CurrentTarget.Type}");
        }
        
        CurrentTarget = null;
        CurrentPriority = TargetPriority.Wandering;
        CurrentState = PathfindingState.Wandering;
        CurrentPath.Clear();
        _pathRecalculationAttempts = 0; // Reset attempts when abandoning target
        _stuckTimer = 0f; // Reset stuck timer
        _pathfindingTimer = 0f; // Reset pathfinding timer
    }
    
    public bool HasActiveTarget => CurrentTarget != null && CurrentState != PathfindingState.Wandering;
    
    /// <summary>
    /// Adds a PoI to cooldown to prevent immediate re-targeting
    /// </summary>
    public void AddPoIToCooldown(PointOfInterest poi, float cooldownTime)
    {
        _poiDetector.AddPoIToCooldown(poi, cooldownTime);
    }
    
    /// <summary>
    /// Finds quest target PoIs within detection range for quest objectives
    /// </summary>
    private PointOfInterest GetQuestTargetPoI(QuestManager questManager, PoIManager poiManager, Vector2 currentPosition)
    {
        var activeQuests = questManager.GetActiveQuests();
        
        foreach (var quest in activeQuests)
        {
            var currentObjective = quest.GetCurrentObjective();
            if (currentObjective?.Type == QuestObjectiveType.VisitLocation)
            {
                // Check if objective specifies a PoI type
                if (currentObjective.Parameters.ContainsKey("poi_type"))
                {
                    var requiredPoiType = (PoIType)currentObjective.Parameters["poi_type"];
                    
                    // Find PoIs of this type within extended quest detection range
                    var nearbyPoIs = poiManager.GetNearbyPoIs(currentPosition, PathfindingConfig.QUEST_TARGET_BIAS_RANGE);
                    var questTargetPoI = nearbyPoIs.FirstOrDefault(poi => 
                        poi.Type == requiredPoiType && 
                        !_poiDetector.IsPoIOnCooldown(poi));
                    
                    if (questTargetPoI != null)
                    {
                        float distance = Vector2.Distance(currentPosition, questTargetPoI.Position);
                        System.Console.WriteLine($"[QUEST TARGET] Found {questTargetPoI.Type} for quest '{quest.Name}' at distance {distance:F1}");
                        return questTargetPoI;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the current pathfinding status for debugging and UI purposes
    /// </summary>
    public string GetPathfindingStatus()
    {
        if (CurrentTarget == null)
            return "Wandering";
            
        string priorityText = CurrentPriority switch
        {
            TargetPriority.Quest => "Quest Target",
            TargetPriority.Discovered => "Discovered PoI",
            _ => "Unknown"
        };
        
        string stateText = CurrentState switch
        {
            PathfindingState.Pathfinding => "Calculating Path",
            PathfindingState.Moving => $"Moving ({CurrentPath.Count} waypoints)",
            _ => "Wandering"
        };
        
        return $"{priorityText}: {CurrentTarget.Type} - {stateText}";
    }
    
    /// <summary>
    /// Provides smooth transition back to wandering with natural direction changes
    /// </summary>
    public Vector2 GetSmoothTransitionDirection(Vector2 currentDirection)
    {
        // When transitioning back to wandering, provide a smooth direction change
        // rather than an abrupt stop
        if (CurrentState == PathfindingState.Wandering && CurrentTarget == null)
        {
            // Add slight randomization to prevent repetitive movement patterns
            Random random = new Random();
            float angleVariation = (float)(random.NextDouble() - 0.5) * 0.5f; // ±0.25 radians
            
            Matrix rotation = Matrix.CreateRotationZ(angleVariation);
            Vector2 smoothDirection = Vector2.Transform(currentDirection, rotation);
            
            if (smoothDirection.Length() > 0)
            {
                smoothDirection.Normalize();
                return smoothDirection;
            }
        }
        
        return currentDirection;
    }
}