using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class WeatherManager : ISaveable
{
    public WeatherType CurrentWeather { get; private set; }
    public float WeatherIntensity { get; private set; } // 0.0 to 1.0
    
    // Weather events
    public event Action<WeatherType> WeatherChanged;
    
    private TimeManager _timeManager;
    private Random _random;
    private int _randomSeed;
    private float _weatherChangeTimer;
    private float _nextWeatherChange;

    public WeatherManager(TimeManager timeManager, int seed = 0)
    {
        _timeManager = timeManager;
        _randomSeed = seed == 0 ? Environment.TickCount : seed;
        _random = new Random(_randomSeed);
        
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
            
            System.Console.WriteLine($"Weather changed to {weather} (intensity: {intensity:F2}) in {_timeManager.GetSeasonName()}");
        }
    }

    public Color GetWeatherTint()
    {
        return CurrentWeather switch
        {
            WeatherType.Rain => Color.Lerp(Color.Transparent, new Color(100, 120, 140), WeatherIntensity * 0.3f),
            WeatherType.Snow => Color.Lerp(Color.Transparent, new Color(240, 240, 255), WeatherIntensity * 0.2f),
            WeatherType.Fog => Color.Lerp(Color.Transparent, new Color(200, 200, 200), WeatherIntensity * 0.4f),
            WeatherType.Cloudy => Color.Lerp(Color.Transparent, new Color(180, 180, 180), WeatherIntensity * 0.2f),
            _ => Color.Transparent
        };
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

    // ISaveable implementation
    public string SaveKey => "WeatherManager";
    public int SaveVersion => 1;

    public object GetSaveData()
    {
        return new WeatherManagerSaveData
        {
            CurrentWeather = CurrentWeather,
            WeatherIntensity = WeatherIntensity,
            WeatherChangeTimer = _weatherChangeTimer,
            NextWeatherChange = _nextWeatherChange,
            RandomSeed = _randomSeed
        };
    }

    public void LoadSaveData(object data)
    {
        if (data is WeatherManagerSaveData saveData)
        {
            CurrentWeather = saveData.CurrentWeather;
            WeatherIntensity = saveData.WeatherIntensity;
            _weatherChangeTimer = saveData.WeatherChangeTimer;
            _nextWeatherChange = saveData.NextWeatherChange;
            _randomSeed = saveData.RandomSeed;
            
            // Recreate Random with saved seed to maintain deterministic behavior
            // Note: The Random state cannot be perfectly restored, but using the same seed
            // will ensure deterministic behavior from this point forward
            _random = new Random(_randomSeed);
        }
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