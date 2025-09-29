using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for the Adventurer class
    /// Contains all necessary state information to restore the adventurer's exact state
    /// </summary>
    public class AdventurerSaveData
    {
        // Position and movement state
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Direction { get; set; }
        public float Speed { get; set; }
        public float DirectionChangeTimer { get; set; }
        public float DirectionChangeInterval { get; set; }
        
        // Interaction state
        public bool IsInteracting { get; set; }
        public float InteractionTimer { get; set; }
        public float InteractionDuration { get; set; }
        public Guid? CurrentInteractionPoIId { get; set; }
        public Guid? LastInteractionPoIId { get; set; }
        public float InteractionCooldownTimer { get; set; }
        
        // Animation state
        public AnimationType CurrentAnimation { get; set; }
        public int CurrentFrame { get; set; }
        public float AnimationTimer { get; set; }
        public bool IsMoving { get; set; }
        
        // Stuck detection state
        public Vector2 LastPosition { get; set; }
        public float StuckTimer { get; set; }
    }
}