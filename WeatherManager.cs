using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class WeatherManager
{
    public WeatherType CurrentWeather { get; private set; }
    public float WeatherIntensity { get; private set; } // 0.0 to 1.0
    
    // Weather events
    public event Action<WeatherType> WeatherChanged;
    
    private TimeManager _timeManager;
    private Random _random;
    private float _weatherChangeTimer;
    private float _nextWeatherChange;

    public WeatherManager(TimeManager timeManager, int seed = 0)
    {
        _timeManager = timeManager;
        _random = seed == 0 ? new Random() : new Random(seed);
        
        CurrentWeather = WeatherType.Clear;
        WeatherIntensity = 0f;
        
        // Subscribe to time events
        _timeManager.HourPassed += OnHourPassed;
        _timeManager.TimeOfDayChanged += OnTimeOfDayChanged;
        
        // Set initial weather change time (shorter for testing)
        _nextWeatherChange = _random.Next(1, 3); // 1-3 hours
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _weatherChangeTimer += deltaTime;
        
        // Check if it's time for weather to change
        float hoursElapsed = _weatherChangeTimer / (_timeManager.DayDuration + _timeManager.NightDuration) * 24f;
        
        if (hoursElapsed >= _nextWeatherChange)
        {
            ChangeWeather();
            _weatherChangeTimer = 0f;
            _nextWeatherChange = _random.Next(1, 4); // Next change in 1-4 hours
        }
    }

    private void OnHourPassed(float gameHour)
    {
        // Weather can change based on time of day
        // Example: More likely to rain in the evening
        if (gameHour >= 18f && gameHour <= 20f) // 6-8 PM
        {
            if (_random.NextDouble() < 0.1) // 10% chance per hour
            {
                if (CurrentWeather == WeatherType.Clear)
                {
                    SetWeather(WeatherType.Rain, 0.3f + (float)_random.NextDouble() * 0.4f);
                }
            }
        }
    }

    private void OnTimeOfDayChanged(TimeOfDay timeOfDay)
    {
        // Weather patterns can change with day/night cycle
        if (timeOfDay == TimeOfDay.Day)
        {
            // Morning tends to clear up weather
            if (CurrentWeather == WeatherType.Rain && _random.NextDouble() < 0.3)
            {
                SetWeather(WeatherType.Clear, 0f);
            }
        }
    }

    private void ChangeWeather()
    {
        // Simple weather generation based on season and current weather
        int season = _timeManager.GetSeason();
        WeatherType newWeather = GenerateWeatherForSeason(season);
        float intensity = _random.NextSingle() * 0.8f + 0.2f; // 0.2 to 1.0
        
        SetWeather(newWeather, intensity);
    }

    private WeatherType GenerateWeatherForSeason(int season)
    {
        return season switch
        {
            0 => // Spring - more rain
                _random.NextDouble() switch
                {
                    < 0.4 => WeatherType.Clear,
                    < 0.7 => WeatherType.Rain,
                    < 0.9 => WeatherType.Cloudy,
                    _ => WeatherType.Fog
                },
            1 => // Summer - mostly clear
                _random.NextDouble() switch
                {
                    < 0.7 => WeatherType.Clear,
                    < 0.85 => WeatherType.Cloudy,
                    _ => WeatherType.Rain
                },
            2 => // Autumn - foggy and rainy
                _random.NextDouble() switch
                {
                    < 0.3 => WeatherType.Clear,
                    < 0.5 => WeatherType.Rain,
                    < 0.8 => WeatherType.Fog,
                    _ => WeatherType.Cloudy
                },
            3 => // Winter - snow and clouds
                _random.NextDouble() switch
                {
                    < 0.2 => WeatherType.Clear,
                    < 0.4 => WeatherType.Snow,
                    < 0.7 => WeatherType.Cloudy,
                    _ => WeatherType.Fog
                },
            _ => WeatherType.Clear
        };
    }

    private void SetWeather(WeatherType weather, float intensity)
    {
        if (CurrentWeather != weather)
        {
            CurrentWeather = weather;
            WeatherIntensity = intensity;
            WeatherChanged?.Invoke(weather);
            
            var lightingInfluence = GetWeatherLightingInfluence();
            System.Console.WriteLine($"Weather changed to {weather} (intensity: {intensity:F2}) in {_timeManager.GetSeasonName()} - Lighting: R{lightingInfluence.R} G{lightingInfluence.G} B{lightingInfluence.B}");
        }
    }

    /// <summary>
    /// Gets weather influence on ambient lighting (used by lighting system)
    /// Returns a multiplier color that affects the base ambient lighting
    /// </summary>
    public Color GetWeatherLightingInfluence()
    {
        return CurrentWeather switch
        {
            WeatherType.Rain => new Color(0.8f, 0.9f, 1.0f) * (0.7f + WeatherIntensity * 0.2f), // Darker, bluer
            WeatherType.Snow => new Color(0.9f, 0.9f, 1.0f) * (0.8f + WeatherIntensity * 0.1f), // Slightly darker, whiter
            WeatherType.Fog => new Color(0.85f, 0.85f, 0.85f) * (0.6f + WeatherIntensity * 0.3f), // Much darker, grayer
            WeatherType.Cloudy => new Color(0.9f, 0.9f, 0.9f) * (0.8f + WeatherIntensity * 0.1f), // Slightly darker
            _ => Color.White // Clear weather - no change
        };
    }

    /// <summary>
    /// Legacy method - now redirects to GetWeatherLightingInfluence for compatibility
    /// </summary>
    [Obsolete("Use GetWeatherLightingInfluence() instead - weather effects are now handled through ambient lighting")]
    public Color GetWeatherTint()
    {
        // Return transparent since we no longer use overlay tinting
        return Color.Transparent;
    }

    public string GetWeatherDescription()
    {
        string intensityDesc = WeatherIntensity switch
        {
            < 0.3f => "Light",
            < 0.7f => "Medium", 
            _ => "Heavy"
        };

        return CurrentWeather switch
        {
            WeatherType.Rain => $"{intensityDesc} Rain",
            WeatherType.Snow => $"{intensityDesc} Snow",
            WeatherType.Fog => $"{intensityDesc} Fog",
            WeatherType.Cloudy => "Cloudy",
            _ => "Clear"
        };
    }
}

public enum WeatherType
{
    Clear,
    Cloudy,
    Rain,
    Snow,
    Fog
}