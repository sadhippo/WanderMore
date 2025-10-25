using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class TimeManager
{
    // Clock configuration - how fast time passes
    public float DayLengthInSeconds { get; set; } = 120f; // 24 game hours = 2 real minutes
    
    // Time period definitions (in game hours, 0-24)
    private const float DAWN_START = 5.0f;   // 5:00 AM
    private const float DAY_START = 6.0f;    // 6:00 AM  
    private const float DUSK_START = 18.0f;  // 6:00 PM
    private const float NIGHT_START = 19.0f; // 7:00 PM
    
    // Current time state
    public float CurrentTime { get; private set; } // Total elapsed seconds
    public float CurrentGameHour { get; private set; } // 0.0 to 24.0
    public TimeOfDay CurrentTimeOfDay { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    
    // Time events for other systems to subscribe to
    public event Action<TimeOfDay> TimeOfDayChanged;
    public event Action<int> DayChanged;
    public event Action<float> HourPassed; // Fires every game hour for regular updates
    public event Action<int> WeekPassed;   // Fires every 7 days for weekly events
    
    private TimeOfDay _previousTimeOfDay;
    private float _lastHourCheck;
    private int _lastWeekCheck;

    public TimeManager()
    {
        CurrentTimeOfDay = TimeOfDay.Dawn;
        _previousTimeOfDay = TimeOfDay.Dawn;
        CurrentTime = 0f;
        CurrentGameHour = DAWN_START; // Start at dawn
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CurrentTime += deltaTime;
        
        // Calculate current game hour (0-24, wraps around)
        float hoursPerSecond = 24f / DayLengthInSeconds;
        CurrentGameHour = (CurrentTime * hoursPerSecond) % 24f;
        
        // Determine time of day based on game hour
        CurrentTimeOfDay = CurrentGameHour switch
        {
            >= DAWN_START and < DAY_START => TimeOfDay.Dawn,
            >= DAY_START and < DUSK_START => TimeOfDay.Day,
            >= DUSK_START and < NIGHT_START => TimeOfDay.Dusk,
            _ => TimeOfDay.Night // Night covers 7 PM to 5 AM (wraps around midnight)
        };
        
        // Check for time of day changes
        if (CurrentTimeOfDay != _previousTimeOfDay)
        {
            TimeOfDayChanged?.Invoke(CurrentTimeOfDay);
            _previousTimeOfDay = CurrentTimeOfDay;
        }
        
        // Check for day changes (when we pass midnight)
        int newDay = (int)(CurrentTime * (24f / DayLengthInSeconds) / 24f) + 1;
        if (newDay != CurrentDay)
        {
            CurrentDay = newDay;
            DayChanged?.Invoke(CurrentDay);
            System.Console.WriteLine($"New day started! Day {CurrentDay}");
        }
        
        // Check for hourly events
        if (Math.Floor(CurrentGameHour) != Math.Floor(_lastHourCheck))
        {
            HourPassed?.Invoke(CurrentGameHour);
        }
        _lastHourCheck = CurrentGameHour;
        
        // Check for weekly events
        int currentWeek = CurrentDay / 7;
        if (currentWeek != _lastWeekCheck)
        {
            WeekPassed?.Invoke(currentWeek);
            _lastWeekCheck = currentWeek;
        }
    }

    public string GetTimeString()
    {
        int hour = (int)CurrentGameHour;
        int minute = (int)((CurrentGameHour - hour) * 60);
        
        if (hour == 0)
        {
            return $"12:{minute:D2} AM";
        }
        else if (hour < 12)
        {
            return $"{hour}:{minute:D2} AM";
        }
        else if (hour == 12)
        {
            return $"12:{minute:D2} PM";
        }
        else
        {
            return $"{hour - 12}:{minute:D2} PM";
        }
    }

    public Color GetAmbientColor()
    {
        return CurrentTimeOfDay switch
        {
            // Dawn: Transition from night to day colors based on progress through dawn period
            TimeOfDay.Dawn => Color.Lerp(
                new Color(20, 30, 60), 
                new Color(255, 150, 100), 
                (CurrentGameHour - DAWN_START) / (DAY_START - DAWN_START)
            ),
            
            // Day: Warm golden sunlight
            TimeOfDay.Day => new Color(255, 220, 180),
            
            // Dusk: Transition from day to night colors based on progress through dusk period
            TimeOfDay.Dusk => Color.Lerp(
                new Color(255, 220, 180), 
                new Color(20, 30, 60), 
                (CurrentGameHour - DUSK_START) / (NIGHT_START - DUSK_START)
            ),
            
            // Night: Dark blue-purple ambient
            TimeOfDay.Night => new Color(20, 30, 60),
            
            _ => Color.White
        };
    }

    public void SetDayLength(float seconds)
    {
        DayLengthInSeconds = seconds;
    }

    public void SetTime(float gameHour)
    {
        gameHour = MathHelper.Clamp(gameHour, 0f, 24f);
        CurrentTime = (gameHour / 24f) * DayLengthInSeconds;
        CurrentGameHour = gameHour;
    }

    public void SetTime(TimeOfDay timeOfDay, float progress = 0f)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        
        float targetHour = timeOfDay switch
        {
            TimeOfDay.Dawn => DAWN_START + (progress * (DAY_START - DAWN_START)),
            TimeOfDay.Day => DAY_START + (progress * (DUSK_START - DAY_START)),
            TimeOfDay.Dusk => DUSK_START + (progress * (NIGHT_START - DUSK_START)),
            TimeOfDay.Night => NIGHT_START + (progress * (24f + DAWN_START - NIGHT_START)) % 24f,
            _ => 6f
        };
        
        SetTime(targetHour);
    }

    // Utility methods for other systems
    public float GetCurrentGameHour()
    {
        return CurrentGameHour;
    }

    public bool IsTimeInRange(float startHour, float endHour)
    {
        float currentHour = GetCurrentGameHour();
        if (startHour <= endHour)
        {
            return currentHour >= startHour && currentHour <= endHour;
        }
        else
        {
            // Handle overnight ranges (e.g., 22:00 to 6:00)
            return currentHour >= startHour || currentHour <= endHour;
        }
    }

    public int GetSeason()
    {
        // Returns 0-3 for Spring, Summer, Autumn, Winter (30 days each)
        return (CurrentDay - 1) / 30 % 4;
    }

    public string GetSeasonName()
    {
        return GetSeason() switch
        {
            0 => "Spring",
            1 => "Summer", 
            2 => "Autumn",
            3 => "Winter",
            _ => "Spring"
        };
    }
}

public enum TimeOfDay
{
    Dawn,
    Day,
    Dusk,
    Night
}