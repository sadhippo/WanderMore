namespace HiddenHorizons;

public static class PathfindingConfig
{
    public const float DETECTION_RANGE = 160f; // 5 tiles (32px per tile)
    
    public const float INTERACTION_COOLDOWN = 8f; // 8 seconds (reduced for more activity)
    public const int MAX_PATH_LENGTH = 5; // Maximum waypoints (increased for complex paths)
    public const float PATH_RECALC_DISTANCE = 160f; // 5 tiles off-path triggers recalc (much more forgiving)
    public const bool PATHFINDING_ENABLED = true; // Master enable/disable
    
    // Additional configuration for fine-tuning
    public const float STUCK_DETECTION_TIME = 2f; // Time before considering stuck (faster detection)
    public const float PATH_TIMEOUT = 30f; // Maximum time to spend on one path (increased)
    public const float MINIMUM_PROGRESS_DISTANCE = 5f; // Minimum distance to consider progress (more sensitive)
    
    // Quest integration settings
    public const float QUEST_TARGET_BIAS_RANGE = 400f; // 12.5 tiles - larger range for quest targets
    public const float QUEST_TARGET_PRIORITY_MULTIPLIER = 2f; // Quest targets get higher priority
}