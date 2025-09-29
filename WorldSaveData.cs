namespace HiddenHorizons
{
    /// <summary>
    /// Composite save data structure containing all world system states
    /// </summary>
    public class WorldSaveData
    {
        public TimeManagerSaveData TimeData { get; set; }
        public WeatherManagerSaveData WeatherData { get; set; }
        public ZoneManagerSaveData ZoneData { get; set; }
    }
}