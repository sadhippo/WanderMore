using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class TimeManager
{
    // Configurable timing (in seconds)
    public float DawnDuration { get; set; } = 60f;   // 1 minute
    public float DayDuration { get; set; } = 240f;   // 4 minutes
    public float DuskDuration { get; set; } = 60f;   // 1 minute
    public float NightDuration { get; set; } = 120f; // 2 minutes
    
    // Current time state
    public float CurrentTime { get; private set; }
    public TimeOfDay CurrentTimeOfDay { get; private set; }
    public float TimeProgress { get; private set; } // 0.0 to 1.0 within current time period
    public int CurrentDay { get; private set; } = 1;
    
    // Time events for other systems to subscribe to
    public event Action<TimeOfDay> TimeOfDayChanged;
    public event Action<int> DayChanged;
    public event Action<float> HourPassed; // Fires every game hour for regular updates
    public event Action<int> WeekPassed;   // Fires every 7 days for weekly events
    
    private float _totalCycleTime;
    private TimeOfDay _previousTimeOfDay;
    private float _lastHourCheck;
    private int _lastWeekCheck;

    public TimeManager()
    {
        _totalCycleTime = DawnDuration + DayDuration + DuskDuration + NightDuration;
        CurrentTimeOfDay = TimeOfDay.Dawn;
        _previousTimeOfDay = TimeOfDay.Dawn;
        CurrentTime = 0f;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CurrentTime += deltaTime;
        
        // Calculate current position in the cycle
        float cyclePosition = CurrentTime % _totalCycleTime;
        
        // Determine time of day and progress
        if (cyclePosition < DawnDuration)
        {
            CurrentTimeOfDay = TimeOfDay.Dawn;
            TimeProgress = cyclePosition / DawnDuration;
        }
        else if (cyclePosition < DawnDuration + DayDuration)
        {
            CurrentTimeOfDay = TimeOfDay.Day;
            TimeProgress = (cyclePosition - DawnDuration) / DayDuration;
        }
        else if (cyclePosition < DawnDuration + DayDuration + DuskDuration)
        {
            CurrentTimeOfDay = TimeOfDay.Dusk;
            TimeProgress = (cyclePosition - DawnDuration - DayDuration) / DuskDuration;
        }
        else
        {
            CurrentTimeOfDay = TimeOfDay.Night;
            TimeProgress = (cyclePosition - DawnDuration - DayDuration - DuskDuration) / NightDuration;
        }
        
        // Check for time of day changes
        if (CurrentTimeOfDay != _previousTimeOfDay)
        {
            TimeOfDayChanged?.Invoke(CurrentTimeOfDay);
            
            // If transitioning from night to dawn, increment day counter
            if (CurrentTimeOfDay == TimeOfDay.Dawn && _previousTimeOfDay == TimeOfDay.Night)
            {
                CurrentDay++;
                DayChanged?.Invoke(CurrentDay);
                System.Console.WriteLine($"New day started! Day {CurrentDay}");
            }
            
            _previousTimeOfDay = CurrentTimeOfDay;
        }
        
        // Check for hourly events (useful for weather changes, hunger, etc.)
        float currentGameHour = GetCurrentGameHour();
        if (Math.Floor(currentGameHour) != Math.Floor(_lastHourCheck))
        {
            HourPassed?.Invoke(currentGameHour);
        }
        _lastHourCheck = currentGameHour;
        
        // Check for weekly events (useful for seasonal changes, etc.)
        int currentWeek = CurrentDay / 7;
        if (currentWeek != _lastWeekCheck)
        {
            WeekPassed?.Invoke(currentWeek);
            _lastWeekCheck = currentWeek;
        }
    }

    public string GetTimeString()
    {
        float currentHour;
        int displayHour, minute;
        
        switch (CurrentTimeOfDay)
        {
            case TimeOfDay.Dawn:
                // Dawn: 5:00 AM to 6:00 AM
                currentHour = 5f + (TimeProgress * 1f);
                displayHour = (int)currentHour;
                minute = (int)((currentHour - displayHour) * 60);
                return $"{displayHour}:{minute:D2} AM";
                
            case TimeOfDay.Day:
                // Day: 6:00 AM to 6:00 PM
                currentHour = 6f + (TimeProgress * 12f);
                displayHour = (int)currentHour;
                minute = (int)((currentHour - displayHour) * 60);
                if (displayHour > 12)
                    return $"{displayHour - 12}:{minute:D2} PM";
                else if (displayHour == 12)
                    return $"12:{minute:D2} PM";
                else
                    return $"{displayHour}:{minute:D2} AM";
                    
            case TimeOfDay.Dusk:
                // Dusk: 6:00 PM to 7:00 PM
                currentHour = 6f + (TimeProgress * 1f);
                displayHour = (int)currentHour;
                minute = (int)((currentHour - displayHour) * 60);
                return $"{displayHour}:{minute:D2} PM";
                
            case TimeOfDay.Night:
                // Night: 7:00 PM to 5:00 AM
                currentHour = 7f + (TimeProgress * 10f);
                if (currentHour >= 12f)
                {
                    // After midnight
                    displayHour = (int)(currentHour - 12f);
                    if (displayHour == 0) displayHour = 12;
                    minute = (int)((currentHour - (int)currentHour) * 60);
                    return $"{displayHour}:{minute:D2} AM";
                }
                else
                {
                    // Before midnight
                    displayHour = (int)currentHour;
                    minute = (int)((currentHour - displayHour) * 60);
                    return $"{displayHour}:{minute:D2} PM";
                }
                
            default:
                return "12:00 AM";
        }
    }

    public Color GetAmbientColor()
    {
        return CurrentTimeOfDay switch
        {
            // Dawn: Transition from night colors to warm orange-pink
            TimeOfDay.Dawn => Color.Lerp(new Color(20, 30, 60), new Color(255, 150, 100), TimeProgress),
            
            // Day: Warm golden sunlight (smooth transition from dawn)
            TimeOfDay.Day => new Color(255, 220, 180), // Warm golden (closer to dawn end color)
            
            // Dusk: Transition from day colors all the way to night colors
            TimeOfDay.Dusk => Color.Lerp(new Color(255, 220, 180), new Color(20, 30, 60), TimeProgress),
            
            // Night: Dark blue-purple ambient (same as end of dusk)
            TimeOfDay.Night => new Color(20, 30, 60),
            
            _ => Color.White
        };
    }

    public void SetDawnDuration(float seconds)
    {
        DawnDuration = seconds;
        _totalCycleTime = DawnDuration + DayDuration + DuskDuration + NightDuration;
    }

    public void SetDayDuration(float seconds)
    {
        DayDuration = seconds;
        _totalCycleTime = DawnDuration + DayDuration + DuskDuration + NightDuration;
    }

    public void SetDuskDuration(float seconds)
    {
        DuskDuration = seconds;
        _totalCycleTime = DawnDuration + DayDuration + DuskDuration + NightDuration;
    }

    public void SetNightDuration(float seconds)
    {
        NightDuration = seconds;
        _totalCycleTime = DawnDuration + DayDuration + DuskDuration + NightDuration;
    }

    public void SetTime(TimeOfDay timeOfDay, float progress = 0f)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn:
                CurrentTime = progress * DawnDuration;
                break;
            case TimeOfDay.Day:
                CurrentTime = DawnDuration + (progress * DayDuration);
                break;
            case TimeOfDay.Dusk:
                CurrentTime = DawnDuration + DayDuration + (progress * DuskDuration);
                break;
            case TimeOfDay.Night:
                CurrentTime = DawnDuration + DayDuration + DuskDuration + (progress * NightDuration);
                break;
        }
    }

    // Utility methods for other systems
    public float GetCurrentGameHour()
    {
        // Returns 0-24 hour format (5 = 5 AM dawn start, 6 = day start, 18 = dusk start, 19 = night start)
        return CurrentTimeOfDay switch
        {
            TimeOfDay.Dawn => 5f + (TimeProgress * 1f),
            TimeOfDay.Day => 6f + (TimeProgress * 12f),
            TimeOfDay.Dusk => 18f + (TimeProgress * 1f),
            TimeOfDay.Night => 19f + (TimeProgress * 10f),
            _ => 6f
        };
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