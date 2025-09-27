using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HiddenHorizons;

public class UIManager
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private TimeManager _timeManager;
    private Rectangle _clockArea;
    private Rectangle _dayCounterArea;

    public UIManager(GraphicsDevice graphicsDevice, TimeManager timeManager)
    {
        _timeManager = timeManager;
        
        // Create pixel texture for UI backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Define UI areas (top-left corner)
        _clockArea = new Rectangle(10, 10, 200, 40);
        _dayCounterArea = new Rectangle(10, 55, 200, 30);
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
        
        // Draw time progress bar
        DrawTimeProgressBar(spriteBatch);
        
        // Draw simple visual time indicators (no text for now)
        DrawTimeIndicators(spriteBatch);
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