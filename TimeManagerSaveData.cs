using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for TimeManager state
    /// </summary>
    public class TimeManagerSaveData
    {
        public float CurrentTime { get; set; }
        public TimeOfDay CurrentTimeOfDay { get; set; }
        public float DayProgress { get; set; }
        public int CurrentDay { get; set; }
        public float DayDuration { get; set; }
        public float NightDuration { get; set; }
    }
}