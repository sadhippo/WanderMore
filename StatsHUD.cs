using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HiddenHorizons;

public class StatsHUD
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _hudArea;
    private Rectangle[] _statBars;
    private string[] _statLabels;
    private Color[] _statColors;
    private string _currentTooltip;
    private Vector2 _tooltipPosition;
    
    // Bar dimensions
    private const int BAR_WIDTH = 120;
    private const int BAR_HEIGHT = 12;
    private const int BAR_SPACING = 18;
    private const int LABEL_WIDTH = 60;
    private const int MARGIN = 5;

    public StatsHUD(GraphicsDevice graphicsDevice)
    {
        // Create pixel texture for drawing bars
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Position HUD below existing UI elements (weather area ends at Y=160)
        int startY = 170;
        _hudArea = new Rectangle(10, startY, LABEL_WIDTH + BAR_WIDTH + MARGIN * 3, BAR_SPACING * 5 + MARGIN * 2);
        
        // Initialize stat bars
        _statBars = new Rectangle[5];
        _statLabels = new string[] { "XP", "Hunger", "Rest", "Comfort", "Mood" };
        _statColors = new Color[]
        {
            new Color(100, 200, 255), // XP - Light blue
            new Color(255, 150, 50),  // Hunger - Orange
            new Color(150, 255, 150), // Rest - Light green
            new Color(200, 200, 100), // Comfort - Yellow
            new Color(255, 100, 200)  // Mood - Pink
        };
        
        for (int i = 0; i < _statBars.Length; i++)
        {
            _statBars[i] = new Rectangle(
                _hudArea.X + LABEL_WIDTH + MARGIN * 2,
                _hudArea.Y + MARGIN + (i * BAR_SPACING),
                BAR_WIDTH,
                BAR_HEIGHT
            );
        }
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void Draw(SpriteBatch spriteBatch, AdventurerStats stats)
    {
        // Draw HUD background
        DrawUIPanel(spriteBatch, _hudArea, new Color(0, 0, 0, 150));
        
        // Draw stat bars
        DrawStatBar(spriteBatch, 0, GetExperiencePercent(stats), stats.Experience, stats.ExperienceToNext);
        DrawStatBar(spriteBatch, 1, stats.Hunger / 100f, stats.Hunger, 100f);
        DrawStatBar(spriteBatch, 2, stats.Tiredness / 100f, stats.Tiredness, 100f);
        DrawStatBar(spriteBatch, 3, stats.Comfort / 100f, stats.Comfort, 100f);
        DrawStatBar(spriteBatch, 4, stats.Mood / 100f, stats.Mood, 100f);
        
        // Draw level indicator
        if (_font != null)
        {
            string levelText = $"Lv.{stats.Level}";
            Vector2 levelPos = new Vector2(_hudArea.X + MARGIN, _hudArea.Y - 20);
            spriteBatch.DrawString(_font, levelText, levelPos, Color.White);
        }
        
        // Draw tooltip if hovering
        if (!string.IsNullOrEmpty(_currentTooltip) && _font != null)
        {
            DrawTooltip(spriteBatch, _currentTooltip, _tooltipPosition);
        }
    }

    private void DrawStatBar(SpriteBatch spriteBatch, int index, float fillPercent, float currentValue, float maxValue)
    {
        Rectangle barArea = _statBars[index];
        Color barColor = _statColors[index];
        
        // Draw background
        spriteBatch.Draw(_pixelTexture, barArea, Color.Black * 0.7f);
        
        // Draw fill
        int fillWidth = (int)(barArea.Width * MathHelper.Clamp(fillPercent, 0f, 1f));
        if (fillWidth > 0)
        {
            Rectangle fillArea = new Rectangle(barArea.X, barArea.Y, fillWidth, barArea.Height);
            
            // Use different shades based on fill level
            Color fillColor = barColor;
            if (fillPercent < 0.25f)
                fillColor = Color.Lerp(Color.Red, barColor, fillPercent * 4f);
            else if (fillPercent < 0.5f)
                fillColor = Color.Lerp(Color.Orange, barColor, (fillPercent - 0.25f) * 4f);
            
            spriteBatch.Draw(_pixelTexture, fillArea, fillColor);
        }
        
        // Draw border
        DrawBorder(spriteBatch, barArea, Color.White * 0.8f, 1);
        
        // Draw label
        if (_font != null)
        {
            Vector2 labelPos = new Vector2(
                _hudArea.X + MARGIN,
                barArea.Y + (barArea.Height - _font.LineSpacing) / 2
            );
            spriteBatch.DrawString(_font, _statLabels[index], labelPos, Color.White);
        }
    }

    private float GetExperiencePercent(AdventurerStats stats)
    {
        if (stats.ExperienceToNext <= 0) return 1f;
        return stats.Experience / stats.ExperienceToNext;
    }

    public bool HandleMouseHover(Vector2 mousePosition, AdventurerStats stats, out string tooltip)
    {
        tooltip = null;
        _currentTooltip = null;
        
        // Check if mouse is over any stat bar
        for (int i = 0; i < _statBars.Length; i++)
        {
            if (_statBars[i].Contains(mousePosition))
            {
                tooltip = GetStatTooltip(i, stats);
                _currentTooltip = tooltip;
                _tooltipPosition = mousePosition;
                return true;
            }
        }
        
        return false;
    }

    private string GetStatTooltip(int statIndex, AdventurerStats stats)
    {
        return statIndex switch
        {
            0 => $"Experience: {stats.Experience:F0}/{stats.ExperienceToNext:F0}\nLevel {stats.Level} ({stats.TotalExperience:F0} total XP)",
            1 => $"Hunger: {stats.Hunger:F0}/100\n{GetHungerDescription(stats.Hunger)}",
            2 => $"Rest: {stats.Tiredness:F0}/100\n{GetRestDescription(stats.Tiredness)}",
            3 => $"Comfort: {stats.Comfort:F0}/100\n{GetComfortDescription(stats.Comfort)}",
            4 => $"Mood: {stats.Mood:F0}/100\n{GetMoodDescription(stats.Mood)}",
            _ => ""
        };
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

    private void DrawTooltip(SpriteBatch spriteBatch, string text, Vector2 position)
    {
        if (_font == null) return;
        
        Vector2 textSize = _font.MeasureString(text);
        Rectangle tooltipArea = new Rectangle(
            (int)position.X + 10,
            (int)position.Y - (int)textSize.Y - 10,
            (int)textSize.X + 10,
            (int)textSize.Y + 6
        );
        
        // Draw tooltip background
        spriteBatch.Draw(_pixelTexture, tooltipArea, new Color(0, 0, 0, 200));
        DrawBorder(spriteBatch, tooltipArea, Color.White, 1);
        
        // Draw tooltip text
        Vector2 textPos = new Vector2(tooltipArea.X + 5, tooltipArea.Y + 3);
        spriteBatch.DrawString(_font, text, textPos, Color.White);
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

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}