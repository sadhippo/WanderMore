using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HiddenHorizons;

public class UIManager
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private TimeManager _timeManager;
    private ZoneManager _zoneManager;
    private WeatherManager _weatherManager;
    private Rectangle _clockArea;
    private Rectangle _dayCounterArea;
    private Rectangle _zoneNameArea;
    private Rectangle _weatherArea;

    public UIManager(GraphicsDevice graphicsDevice, TimeManager timeManager, ZoneManager zoneManager, WeatherManager weatherManager)
    {
        _timeManager = timeManager;
        _zoneManager = zoneManager;
        _weatherManager = weatherManager;
        
        // Create pixel texture for UI backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Define UI areas (top-left corner)
        _clockArea = new Rectangle(10, 10, 200, 40);
        _dayCounterArea = new Rectangle(10, 55, 200, 30);
        _zoneNameArea = new Rectangle(10, 90, 250, 35);
        _weatherArea = new Rectangle(10, 130, 220, 30);
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw clock background
        DrawUIPanel(spriteBatch, _clockArea, new Color(0, 0, 0, 150));
        
        // Draw day counter background
        DrawUIPanel(spriteBatch, _dayCounterArea, new Color(0, 0, 0, 120));
        
        // Draw zone name background
        DrawUIPanel(spriteBatch, _zoneNameArea, new Color(0, 0, 0, 100));
        
        // Draw weather background
        DrawUIPanel(spriteBatch, _weatherArea, new Color(0, 0, 0, 120));
        
        // Draw time progress bar
        DrawTimeProgressBar(spriteBatch);
        
        if (_font != null)
        {
            // Draw text-based UI
            DrawTextUI(spriteBatch);
        }
        else
        {
            // Fallback to simple visual indicators
            DrawTimeIndicators(spriteBatch);
        }
    }

    private void DrawTextUI(SpriteBatch spriteBatch)
    {
        // Draw time text
        string timeText = _timeManager.GetTimeString();
        string timeOfDayIcon = _timeManager.CurrentTimeOfDay == TimeOfDay.Day ? "SUN" : "MOON";
        string fullTimeText = $"{timeOfDayIcon} {timeText}";
        
        Vector2 timePosition = new Vector2(_clockArea.X + 10, _clockArea.Y + 8);
        Color timeColor = _timeManager.CurrentTimeOfDay == TimeOfDay.Day ? 
            new Color(255, 220, 100) : new Color(200, 200, 255);
        
        spriteBatch.DrawString(_font, fullTimeText, timePosition, timeColor);
        
        // Draw day counter
        string dayText = $"Day {_timeManager.CurrentDay}";
        Vector2 dayPosition = new Vector2(_dayCounterArea.X + 10, _dayCounterArea.Y + 5);
        spriteBatch.DrawString(_font, dayText, dayPosition, Color.White);
        
        // Draw zone information
        if (_zoneManager.CurrentZone != null)
        {
            string zoneName = _zoneManager.CurrentZone.Name;
            string biomeType = _zoneManager.CurrentZone.BiomeType.ToString();
            string zoneText = $"{zoneName}";
            string biomeText = $"({biomeType})";
            
            Vector2 zonePosition = new Vector2(_zoneNameArea.X + 10, _zoneNameArea.Y + 5);
            Vector2 biomePosition = new Vector2(_zoneNameArea.X + 10, _zoneNameArea.Y + 18);
            
            // Zone name in white
            spriteBatch.DrawString(_font, zoneText, zonePosition, Color.White);
            
            // Biome type in a color based on biome
            Color biomeColor = GetBiomeColor(_zoneManager.CurrentZone.BiomeType);
            spriteBatch.DrawString(_font, biomeText, biomePosition, biomeColor);
        }
        
        // Draw weather information
        string weatherText = _weatherManager.GetWeatherDescription();
        string seasonText = $"{_timeManager.GetSeasonName()}";
        
        Vector2 weatherPosition = new Vector2(_weatherArea.X + 10, _weatherArea.Y + 5);
        Vector2 seasonPosition = new Vector2(_weatherArea.X + 120, _weatherArea.Y + 5);
        
        Color weatherColor = GetWeatherColor(_weatherManager.CurrentWeather);
        spriteBatch.DrawString(_font, weatherText, weatherPosition, weatherColor);
        spriteBatch.DrawString(_font, seasonText, seasonPosition, GetSeasonColor(_timeManager.GetSeason()));
    }

    private void DrawTimeIndicators(SpriteBatch spriteBatch)
    {
        // Draw sun/moon indicator
        Rectangle iconArea = new Rectangle(_clockArea.X + 10, _clockArea.Y + 8, 20, 20);
        Color iconColor = _timeManager.CurrentTimeOfDay == TimeOfDay.Day ? 
            Color.Yellow : Color.LightBlue;
        
        spriteBatch.Draw(_pixelTexture, iconArea, iconColor);
        
        // Draw day counter as simple blocks
        int dayCount = _timeManager.CurrentDay;
        for (int i = 0; i < Math.Min(dayCount, 10); i++) // Show up to 10 days as blocks
        {
            Rectangle dayBlock = new Rectangle(
                _dayCounterArea.X + 10 + (i * 15), 
                _dayCounterArea.Y + 8, 
                10, 10
            );
            spriteBatch.Draw(_pixelTexture, dayBlock, Color.White);
        }
        
        // If more than 10 days, show a larger block
        if (dayCount > 10)
        {
            Rectangle bigBlock = new Rectangle(
                _dayCounterArea.X + 170, 
                _dayCounterArea.Y + 8, 
                15, 15
            );
            spriteBatch.Draw(_pixelTexture, bigBlock, Color.Gold);
        }
        
        // Draw zone indicator (fallback when no font)
        if (_zoneManager.CurrentZone != null)
        {
            Rectangle zoneIndicator = new Rectangle(_zoneNameArea.X + 10, _zoneNameArea.Y + 10, 30, 15);
            Color biomeColor = GetBiomeColor(_zoneManager.CurrentZone.BiomeType);
            spriteBatch.Draw(_pixelTexture, zoneIndicator, biomeColor);
        }
        
        // Draw weather indicator (fallback when no font)
        Rectangle weatherIndicator = new Rectangle(_weatherArea.X + 10, _weatherArea.Y + 8, 25, 15);
        Color weatherColor = GetWeatherColor(_weatherManager.CurrentWeather);
        spriteBatch.Draw(_pixelTexture, weatherIndicator, weatherColor);
        
        // Draw season indicator
        Rectangle seasonIndicator = new Rectangle(_weatherArea.X + 45, _weatherArea.Y + 8, 25, 15);
        Color seasonColor = GetSeasonColor(_timeManager.GetSeason());
        spriteBatch.Draw(_pixelTexture, seasonIndicator, seasonColor);
    }

    private Color GetBiomeColor(BiomeType biomeType)
    {
        return biomeType switch
        {
            BiomeType.Forest => Color.Green,
            BiomeType.DenseForest => Color.DarkGreen,
            BiomeType.Plains => Color.Yellow,
            BiomeType.Lake => Color.CornflowerBlue,
            BiomeType.Mountain => Color.Gray,
            BiomeType.Swamp => Color.DarkOliveGreen,
            _ => Color.White
        };
    }

    private Color GetWeatherColor(WeatherType weatherType)
    {
        return weatherType switch
        {
            WeatherType.Clear => Color.Yellow,
            WeatherType.Cloudy => Color.LightGray,
            WeatherType.Rain => Color.CornflowerBlue,
            WeatherType.Snow => Color.White,
            WeatherType.Fog => Color.Gray,
            _ => Color.White
        };
    }

    private Color GetSeasonColor(int season)
    {
        return season switch
        {
            0 => Color.LightGreen,  // Spring
            1 => Color.Yellow,      // Summer
            2 => Color.Orange,      // Autumn
            3 => Color.LightBlue,   // Winter
            _ => Color.White
        };
    } 
        

    private void DrawUIPanel(SpriteBatch spriteBatch, Rectangle area, Color backgroundColor)
    {
        // Draw background
        spriteBatch.Draw(_pixelTexture, area, backgroundColor);
        
        // Draw border
        DrawBorder(spriteBatch, area, Color.White * 0.5f, 1);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Y, area.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Y, thickness, area.Height), color);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
    }

    private void DrawTimeProgressBar(SpriteBatch spriteBatch)
    {
        Rectangle progressArea = new Rectangle(_clockArea.X + 10, _clockArea.Bottom - 8, _clockArea.Width - 20, 4);
        
        // Background
        spriteBatch.Draw(_pixelTexture, progressArea, Color.Black * 0.5f);
        
        // Progress fill
        int fillWidth = (int)(progressArea.Width * _timeManager.DayProgress);
        Rectangle fillArea = new Rectangle(progressArea.X, progressArea.Y, fillWidth, progressArea.Height);
        
        Color progressColor = _timeManager.CurrentTimeOfDay == TimeOfDay.Day ? 
            Color.Yellow : Color.DarkBlue;
        
        spriteBatch.Draw(_pixelTexture, fillArea, progressColor);
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}

// Extension class for easy time configuration
public static class TimeManagerExtensions
{
    public static void SetDayNightCycle(this TimeManager timeManager, float dayMinutes, float nightMinutes)
    {
        timeManager.SetDayDuration(dayMinutes * 60f);
        timeManager.SetNightDuration(nightMinutes * 60f);
    }
}