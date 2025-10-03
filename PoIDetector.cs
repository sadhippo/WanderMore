using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class PoIDetector
{
    private const float DETECTION_RANGE = 320f; // 10 tiles (32px per tile) - increased for better detection
    private Dictionary<Guid, float> _poiCooldowns;
    private float _lastUpdateTime;
    private int _debugCallCount;
    
    public PoIDetector()
    {
        _poiCooldowns = new Dictionary<Guid, float>();
        _lastUpdateTime = 0f;
        _debugCallCount = 0;
    }
    
    public PointOfInterest FindNearestInterestingPoI(Vector2 position, PoIManager poiManager)
    {
        if (poiManager == null) return null;
        
        // Update cooldowns
        UpdateCooldowns();
        
        // Get nearby PoIs within detection range
        var nearbyPoIs = poiManager.GetNearbyPoIs(position, DETECTION_RANGE);
        
        // Debug output every 100 calls (roughly every 2 seconds)
        if (_debugCallCount++ % 100 == 0)
        {
            System.Console.WriteLine($"[POI DETECTOR] Checking {nearbyPoIs.Count} nearby PoIs at position {position}");
            System.Console.WriteLine($"[POI DETECTOR] Active cooldowns: {_poiCooldowns.Count}");
        }
        
        PointOfInterest bestTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (var poi in nearbyPoIs)
        {
            // Skip if already interacted with (permanent)
            if (poi.HasBeenInteracted)
            {
                if (_debugCallCount % 100 == 0)
                {
                    System.Console.WriteLine($"[POI DETECTOR] Skipping {poi.Type} - already interacted");
                }
                continue;
            }
            
            // Skip if on cooldown
            if (IsPoIOnCooldown(poi))
            {
                if (_debugCallCount % 100 == 0)
                {
                    System.Console.WriteLine($"[POI DETECTOR] Skipping {poi.Type} - on cooldown");
                }
                continue;
            }
                
            // Skip if not interactable
            if (!IsInteractablePoI(poi.Type))
                continue;
            
            float distance = Vector2.Distance(position, poi.Position);
            if (distance < closestDistance)
            {
                bestTarget = poi;
                closestDistance = distance;
            }
        }
        
        if (bestTarget != null && _debugCallCount % 100 == 0)
        {
            System.Console.WriteLine($"[POI DETECTOR] Found target: {bestTarget.Type} at distance {Vector2.Distance(position, bestTarget.Position):F1}");
        }
        
        return bestTarget;
    }
    
    public bool IsPoIOnCooldown(PointOfInterest poi)
    {
        if (poi == null) return true;
        
        return _poiCooldowns.ContainsKey(poi.Id) && _poiCooldowns[poi.Id] > 0f;
    }
    
    public void AddPoIToCooldown(PointOfInterest poi, float cooldownTime)
    {
        if (poi == null) return;
        
        _poiCooldowns[poi.Id] = cooldownTime;
        System.Console.WriteLine($"[POI INTERACTION] {poi.Type} added to cooldown for {cooldownTime} seconds");
    }
    
    private void UpdateCooldowns()
    {
        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;
        float deltaTime = currentTime - _lastUpdateTime;
        
        // Initialize _lastUpdateTime if this is the first call
        if (_lastUpdateTime == 0f)
        {
            _lastUpdateTime = currentTime;
            return;
        }
        
        _lastUpdateTime = currentTime;
        
        // Reduce all cooldowns
        var keysToRemove = new List<Guid>();
        var keysToUpdate = _poiCooldowns.Keys.ToList();
        
        foreach (var key in keysToUpdate)
        {
            _poiCooldowns[key] -= deltaTime;
            if (_poiCooldowns[key] <= 0f)
            {
                keysToRemove.Add(key);
            }
        }
        
        // Remove expired cooldowns
        foreach (var key in keysToRemove)
        {
            _poiCooldowns.Remove(key);
            System.Console.WriteLine($"[POI DETECTOR] Cooldown expired for PoI {key}");
        }
    }
    
    private bool IsInteractablePoI(PoIType poiType)
    {
        return poiType switch
        {
            // Buildings
            PoIType.Inn => true,
            PoIType.Cottage => true,
            PoIType.Farmhouse => true,
            PoIType.Castle => true,
            PoIType.Chapel => true,
            PoIType.Hut => true,
            PoIType.Mine => true,
            
            // NPCs
            PoIType.Ranger => true,
            PoIType.Priest => true,
            PoIType.Warrior => true,
            PoIType.Scholar => true,
            PoIType.Hermit => true,
            PoIType.Adventurer => true,
            PoIType.Mermaid => true,
            
            // Animals
            PoIType.Cat => true,
            PoIType.Dog => true,
            PoIType.Unicorn => true,
            PoIType.Sheep => true,
            PoIType.Chicken => true,
            PoIType.Pig => true,
            PoIType.Deer => true,
            
            // Resources
            PoIType.BerryBush => true,
            
            // Don't interact with monsters or dangerous things
            PoIType.Skeleton => false,
            PoIType.Dragon => false,
            PoIType.Minotaur => false,
            PoIType.Golem => false,
            PoIType.Centaur => false,
            
            _ => false
        };
    }
}