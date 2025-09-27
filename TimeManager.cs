using Microsoft.Xna.Framework;
using System;

namespace HiddenHorizons;

public class TimeManager
{
    // Configurable timing (in seconds)
    public float DayDuration { get; set; } = 300f;   // 5 minutes
    public float NightDuration { get; set; } = 180f; // 3 minutes
    
    // Current time state
    public float CurrentTime { get; private set; }
    public TimeOfDay CurrentTimeOfDay { get; private set; }
    public float DayProgress { get; private set; } // 0.0 to 1.0
    public int CurrentDay { get; private set; } = 1;
    
    // Time events
    public event Action<TimeOfDay> TimeOfDayChanged;
    public event Action<int> DayChanged;
    
    private float _totalCycleTime;
    private TimeOfDay _previousTimeOfDay;

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
}

public enum TimeOfDay
{
    Day,
    Night
}