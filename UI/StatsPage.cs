using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HiddenHorizons;

public class StatsPage
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _pageArea;
    private bool _isOpen;
    
    // Layout constants
    private const int PAGE_WIDTH = 400;
    private const int PAGE_HEIGHT = 500;
    private const int MARGIN = 20;
    private const int LINE_HEIGHT = 20;

    public bool IsOpen => _isOpen;

    public StatsPage(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        // Create pixel texture for drawing
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Center the page on screen
        _pageArea = new Rectangle(
            (screenWidth - PAGE_WIDTH) / 2,
            (screenHeight - PAGE_HEIGHT) / 2,
            PAGE_WIDTH,
            PAGE_HEIGHT
        );
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void Update(GameTime gameTime)
    {
        // Input handling is now done in Game1.cs
        // This method is kept for future updates if needed
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
    }

    public void Draw(SpriteBatch spriteBatch, AdventurerStats stats)
    {
        if (!_isOpen) return;
        
        // Draw page background
        DrawUIPanel(spriteBatch, _pageArea, new Color(20, 20, 30, 240));
        
        if (_font == null)
        {
            // Fallback visual representation when no font is available
            DrawFallbackStats(spriteBatch, stats);
            return;
        }
        
        // Draw title
        string title = "Adventurer Statistics";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            _pageArea.X + (_pageArea.Width - titleSize.X) / 2,
            _pageArea.Y + MARGIN
        );
        spriteBatch.DrawString(_font, title, titlePos, Color.White);
        
        // Draw stats content
        int currentY = _pageArea.Y + MARGIN + (int)titleSize.Y + MARGIN;
        
        // Experience section
        currentY = DrawSection(spriteBatch, "Experience & Level", currentY);
        currentY = DrawStatLine(spriteBatch, $"Level: {stats.Level}", currentY);
        currentY = DrawStatLine(spriteBatch, $"Current XP: {stats.Experience:F0} / {stats.ExperienceToNext:F0}", currentY);
        currentY = DrawStatLine(spriteBatch, $"Total XP Earned: {stats.TotalExperience:F0}", currentY);
        currentY = DrawStatLine(spriteBatch, $"Progress to Next Level: {GetExperiencePercent(stats) * 100:F1}%", currentY);
        currentY += LINE_HEIGHT / 2;
        
        // Physical stats section
        currentY = DrawSection(spriteBatch, "Physical Condition", currentY);
        currentY = DrawStatLine(spriteBatch, $"Hunger: {stats.Hunger:F0}/100 ({GetHungerDescription(stats.Hunger)})", currentY);
        currentY = DrawStatLine(spriteBatch, $"Rest: {stats.Tiredness:F0}/100 ({GetRestDescription(stats.Tiredness)})", currentY);
        currentY += LINE_HEIGHT / 2;
        
        // Mental stats section
        currentY = DrawSection(spriteBatch, "Mental State", currentY);
        currentY = DrawStatLine(spriteBatch, $"Comfort: {stats.Comfort:F0}/100 ({GetComfortDescription(stats.Comfort)})", currentY);
        currentY = DrawStatLine(spriteBatch, $"Mood: {stats.Mood:F0}/100 ({GetMoodDescription(stats.Mood)})", currentY);
        currentY += LINE_HEIGHT;
        
        // Stat descriptions section
        currentY = DrawSection(spriteBatch, "Stat Descriptions", currentY);
        currentY = DrawDescriptionLine(spriteBatch, "Experience: Gained from quests and discoveries", currentY);
        currentY = DrawDescriptionLine(spriteBatch, "Hunger: Decreases over time, restore at inns", currentY);
        currentY = DrawDescriptionLine(spriteBatch, "Rest: Decreases with movement, restore by resting", currentY);
        currentY = DrawDescriptionLine(spriteBatch, "Comfort: Based on hunger, rest, and weather", currentY);
        currentY = DrawDescriptionLine(spriteBatch, "Mood: Influenced by comfort and activities", currentY);
        currentY += LINE_HEIGHT;
        
        // Instructions
        Vector2 instructionPos = new Vector2(_pageArea.X + MARGIN, _pageArea.Bottom - MARGIN - LINE_HEIGHT);
        spriteBatch.DrawString(_font, "Press S to close", instructionPos, Color.Gray);
    }

    private void DrawFallbackStats(SpriteBatch spriteBatch, AdventurerStats stats)
    {
        // Draw simple visual bars when no font is available
        int barY = _pageArea.Y + MARGIN * 2;
        int barWidth = _pageArea.Width - MARGIN * 2;
        int barHeight = 20;
        int barSpacing = 30;
        
        Color[] colors = {
            new Color(100, 200, 255), // XP
            new Color(255, 150, 50),  // Hunger
            new Color(150, 255, 150), // Rest
            new Color(200, 200, 100), // Comfort
            new Color(255, 100, 200)  // Mood
        };
        
        float[] values = {
            GetExperiencePercent(stats),
            stats.Hunger / 100f,
            stats.Tiredness / 100f,
            stats.Comfort / 100f,
            stats.Mood / 100f
        };
        
        for (int i = 0; i < 5; i++)
        {
            Rectangle barArea = new Rectangle(_pageArea.X + MARGIN, barY + (i * barSpacing), barWidth, barHeight);
            
            // Background
            spriteBatch.Draw(_pixelTexture, barArea, Color.Black * 0.5f);
            
            // Fill
            int fillWidth = (int)(barWidth * MathHelper.Clamp(values[i], 0f, 1f));
            if (fillWidth > 0)
            {
                Rectangle fillArea = new Rectangle(barArea.X, barArea.Y, fillWidth, barArea.Height);
                spriteBatch.Draw(_pixelTexture, fillArea, colors[i]);
            }
            
            // Border
            DrawBorder(spriteBatch, barArea, Color.White, 1);
        }
    }

    private int DrawSection(SpriteBatch spriteBatch, string sectionTitle, int y)
    {
        Vector2 pos = new Vector2(_pageArea.X + MARGIN, y);
        spriteBatch.DrawString(_font, sectionTitle, pos, Color.Yellow);
        
        // Draw underline
        Vector2 titleSize = _font.MeasureString(sectionTitle);
        Rectangle underline = new Rectangle(
            (int)pos.X, 
            (int)(pos.Y + titleSize.Y + 2), 
            (int)titleSize.X, 
            1
        );
        spriteBatch.Draw(_pixelTexture, underline, Color.Yellow);
        
        return y + LINE_HEIGHT + 5;
    }

    private int DrawStatLine(SpriteBatch spriteBatch, string text, int y)
    {
        Vector2 pos = new Vector2(_pageArea.X + MARGIN + 10, y);
        spriteBatch.DrawString(_font, text, pos, Color.White);
        return y + LINE_HEIGHT;
    }

    private int DrawDescriptionLine(SpriteBatch spriteBatch, string text, int y)
    {
        Vector2 pos = new Vector2(_pageArea.X + MARGIN + 10, y);
        spriteBatch.DrawString(_font, text, pos, Color.LightGray);
        return y + LINE_HEIGHT;
    }

    private float GetExperiencePercent(AdventurerStats stats)
    {
        if (stats.ExperienceToNext <= 0) return 1f;
        return stats.Experience / stats.ExperienceToNext;
    }

    private string GetHungerDescription(float hunger)
    {
        return hunger switch
        {
            >= 80f => "Well fed",
            >= 60f => "Satisfied",
            >= 40f => "Getting hungry",
            >= 20f => "Quite hungry",
            _ => "Very hungry"
        };
    }

    private string GetRestDescription(float tiredness)
    {
        return tiredness switch
        {
            >= 80f => "Well rested",
            >= 60f => "Energetic",
            >= 40f => "Getting tired",
            >= 20f => "Quite tired",
            _ => "Exhausted"
        };
    }

    private string GetComfortDescription(float comfort)
    {
        return comfort switch
        {
            >= 80f => "Very comfortable",
            >= 60f => "Comfortable",
            >= 40f => "Somewhat comfortable",
            >= 20f => "Uncomfortable",
            _ => "Very uncomfortable"
        };
    }

    private string GetMoodDescription(float mood)
    {
        return mood switch
        {
            >= 80f => "Excellent spirits",
            >= 60f => "Good mood",
            >= 40f => "Neutral",
            >= 20f => "Low spirits",
            _ => "Quite sad"
        };
    }

    private void DrawUIPanel(SpriteBatch spriteBatch, Rectangle area, Color backgroundColor)
    {
        // Draw background
        spriteBatch.Draw(_pixelTexture, area, backgroundColor);
        
        // Draw border
        DrawBorder(spriteBatch, area, Color.White * 0.8f, 2);
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

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}