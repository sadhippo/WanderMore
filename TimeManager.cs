using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class TimeManager : ISaveable
{
    // Configurable timing (in seconds)
    public float DayDuration { get; set; } = 300f;   // 5 minutes
    public float NightDuration { get; set; } = 180f; // 3 minutes
    
    // Current time state
    public float CurrentTime { get; private set; }
    public TimeOfDay CurrentTimeOfDay { get; private set; }
    public float DayProgress { get; private set; } // 0.0 to 1.0
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
        _totalCycleTime = DayDuration + NightDuration;
        CurrentTimeOfDay = TimeOfDay.Day;
        _previousTimeOfDay = TimeOfDay.Day;
        CurrentTime = 0f;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        CurrentTime += deltaTime;
        
        // Calculate current position in the cycle
        float cyclePosition = CurrentTime % _totalCycleTime;
        
        // Determine time of day and progress
        if (cyclePosition < DayDuration)
        {
            CurrentTimeOfDay = TimeOfDay.Day;
            DayProgress = cyclePosition / DayDuration;
        }
        else
        {
            CurrentTimeOfDay = TimeOfDay.Night;
            DayProgress = (cyclePosition - DayDuration) / NightDuration;
        }
        
        // Check for time of day changes
        if (CurrentTimeOfDay != _previousTimeOfDay)
        {
            TimeOfDayChanged?.Invoke(CurrentTimeOfDay);
            
            // If transitioning from night to day, increment day counter
            if (CurrentTimeOfDay == TimeOfDay.Day && _previousTimeOfDay == TimeOfDay.Night)
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
        // Convert progress to 12-hour format
        float totalHours = CurrentTimeOfDay == TimeOfDay.Day ? 12f : 12f;
        float currentHour = DayProgress * totalHours;
        
        int hour = (int)currentHour;
        int minute = (int)((currentHour - hour) * 60);
        
        if (CurrentTimeOfDay == TimeOfDay.Day)
        {
            // Day: 6:00 AM to 6:00 PM
            int displayHour = 6 + hour;
            if (displayHour > 12)
                return $"{displayHour - 12}:{minute:D2} PM";
            else if (displayHour == 12)
                return $"12:{minute:D2} PM";
            else
                return $"{displayHour}:{minute:D2} AM";
        }
        else
        {
            // Night: 6:00 PM to 6:00 AM
            int displayHour = 6 + hour;
            if (displayHour > 12)
                return $"{displayHour - 12}:{minute:D2} AM";
            else if (displayHour == 12)
                return $"12:{minute:D2} AM";
            else
                return $"{displayHour}:{minute:D2} PM";
        }
    }

    public Color GetAmbientColor()
    {
        if (CurrentTimeOfDay == TimeOfDay.Day)
        {
            // Day colors: bright to warm
            if (DayProgress < 0.1f) // Dawn
            {
                return Color.Lerp(new Color(255, 200, 150), Color.White, DayProgress * 10f);
            }
            else if (DayProgress > 0.9f) // Dusk
            {
                float duskProgress = (DayProgress - 0.9f) * 10f;
                return Color.Lerp(Color.White, new Color(255, 180, 120), duskProgress);
            }
            else // Midday
            {
                return Color.White;
            }
        }
        else
        {
            // Night colors: dark blue tints
            if (DayProgress < 0.1f) // Early night
            {
                return Color.Lerp(new Color(255, 180, 120), new Color(150, 150, 200), DayProgress * 10f);
            }
            else if (DayProgress > 0.9f) // Pre-dawn
            {
                float predawnProgress = (DayProgress - 0.9f) * 10f;
                return Color.Lerp(new Color(150, 150, 200), new Color(255, 200, 150), predawnProgress);
            }
            else // Deep night
            {
                return new Color(120, 120, 180);
            }
        }
    }

    public void SetDayDuration(float seconds)
    {
        DayDuration = seconds;
        _totalCycleTime = DayDuration + NightDuration;
    }

    public void SetNightDuration(float seconds)
    {
        NightDuration = seconds;
        _totalCycleTime = DayDuration + NightDuration;
    }

    public void SetTime(TimeOfDay timeOfDay, float progress = 0f)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        
        if (timeOfDay == TimeOfDay.Day)
        {
            CurrentTime = progress * DayDuration;
        }
        else
        {
            CurrentTime = DayDuration + (progress * NightDuration);
        }
    }

    // Utility methods for other systems
    public float GetCurrentGameHour()
    {
        // Returns 0-24 hour format (0 = 6 AM, 12 = 6 PM, 18 = midnight, etc.)
        if (CurrentTimeOfDay == TimeOfDay.Day)
        {
            return DayProgress * 12f; // 0-12 hours of day
        }
        else
        {
            return 12f + (DayProgress * 12f); // 12-24 hours of night
        }
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

    // ISaveable implementation
    public string SaveKey => "TimeManager";
    public int SaveVersion => 1;

    public object GetSaveData()
    {
        return new TimeManagerSaveData
        {
            CurrentTime = CurrentTime,
            CurrentTimeOfDay = CurrentTimeOfDay,
            DayProgress = DayProgress,
            CurrentDay = CurrentDay,
            DayDuration = DayDuration,
            NightDuration = NightDuration
        };
    }

    public void LoadSaveData(object data)
    {
        if (data is TimeManagerSaveData saveData)
        {
            CurrentTime = saveData.CurrentTime;
            CurrentTimeOfDay = saveData.CurrentTimeOfDay;
            DayProgress = saveData.DayProgress;
            CurrentDay = saveData.CurrentDay;
            DayDuration = saveData.DayDuration;
            NightDuration = saveData.NightDuration;
            
            // Recalculate derived values
            _totalCycleTime = DayDuration + NightDuration;
            _previousTimeOfDay = CurrentTimeOfDay;
            _lastHourCheck = GetCurrentGameHour();
            _lastWeekCheck = CurrentDay / 7;
        }
    }
}

public enum TimeOfDay
{
    Day,
    Night
}