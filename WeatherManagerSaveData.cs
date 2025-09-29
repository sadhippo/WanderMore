using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for WeatherManager state
    /// </summary>
    public class WeatherManagerSaveData
    {
        public WeatherType CurrentWeather { get; set; }
        public float WeatherIntensity { get; set; }
        public float WeatherChangeTimer { get; set; }
        public float NextWeatherChange { get; set; }
        public int RandomSeed { get; set; }
    }
}