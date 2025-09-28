using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class JournalUI
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private JournalManager _journalManager;
    private bool _isVisible;
    private Rectangle _journalArea;
    private Rectangle _headerArea;
    private Rectangle _contentArea;
    private Rectangle _statsArea;
    private List<JournalEntry> _displayedEntries;
    private int _scrollOffset;
    private int _maxVisibleEntries;

    // Input handling
    private KeyboardState _previousKeyboardState;

    public bool IsVisible => _isVisible;

    public JournalUI(GraphicsDevice graphicsDevice, JournalManager journalManager)
    {
        _journalManager = journalManager;
        _displayedEntries = new List<JournalEntry>();
        _scrollOffset = 0;
        _maxVisibleEntries = 8;
        
        // Create pixel texture for backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Define journal areas (centered on screen)
        int journalWidth = 600;
        int journalHeight = 500;
        int screenWidth = 1024; // Assuming default screen size
        int screenHeight = 768;
        
        _journalArea = new Rectangle(
            (screenWidth - journalWidth) / 2,
            (screenHeight - journalHeight) / 2,
            journalWidth,
            journalHeight
        );
        
        _headerArea = new Rectangle(_journalArea.X, _journalArea.Y, _journalArea.Width, 40);
        _statsArea = new Rectangle(_journalArea.X, _journalArea.Y + 40, _journalArea.Width, 60);
        _contentArea = new Rectangle(_journalArea.X, _journalArea.Y + 100, _journalArea.Width, _journalArea.Height - 100);
        
        _previousKeyboardState = Keyboard.GetState();
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void Update(GameTime gameTime)
    {
        var currentKeyboardState = Keyboard.GetState();
        
        // Toggle journal with J key
        if (currentKeyboardState.IsKeyDown(Keys.J) && !_previousKeyboardState.IsKeyDown(Keys.J))
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                RefreshEntries();
            }
        }
        
        // Scroll through entries when journal is open
        if (_isVisible)
        {
            if (currentKeyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            {
                _scrollOffset = Math.Max(0, _scrollOffset - 1);
            }
            if (currentKeyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            {
                _scrollOffset = Math.Min(_displayedEntries.Count - _maxVisibleEntries, _scrollOffset + 1);
            }
        }
        
        _previousKeyboardState = currentKeyboardState;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isVisible || _font == null) return;
        
        // Draw semi-transparent background overlay
        Rectangle screenRect = new Rectangle(0, 0, 1024, 768);
        spriteBatch.Draw(_pixelTexture, screenRect, Color.Black * 0.7f);
        
        // Draw journal background
        spriteBatch.Draw(_pixelTexture, _journalArea, new Color(40, 35, 60));
        DrawBorder(spriteBatch, _journalArea, Color.White, 2);
        
        // Draw header
        spriteBatch.Draw(_pixelTexture, _headerArea, new Color(60, 50, 80));
        Vector2 titlePosition = new Vector2(_headerArea.X + 10, _headerArea.Y + 8);
        spriteBatch.DrawString(_font, "~ Adventure Journal ~", titlePosition, Color.White);
        
        // Draw statistics
        DrawStatistics(spriteBatch);
        
        // Draw entries
        DrawEntries(spriteBatch);
        
        // Draw instructions
        Vector2 instructionPos = new Vector2(_journalArea.X + 10, _journalArea.Bottom - 25);
        spriteBatch.DrawString(_font, "Press J to close | Up/Down to scroll", instructionPos, Color.LightGray);
    }

    private void DrawStatistics(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixelTexture, _statsArea, new Color(50, 45, 70));
        
        var stats = _journalManager.GetStatistics();
        
        Vector2 statsPos1 = new Vector2(_statsArea.X + 10, _statsArea.Y + 5);
        Vector2 statsPos2 = new Vector2(_statsArea.X + 10, _statsArea.Y + 25);
        Vector2 statsPos3 = new Vector2(_statsArea.X + 300, _statsArea.Y + 5);
        Vector2 statsPos4 = new Vector2(_statsArea.X + 300, _statsArea.Y + 25);
        
        spriteBatch.DrawString(_font, $"Days Explored: {stats.DaysExplored}", statsPos1, Color.LightBlue);
        spriteBatch.DrawString(_font, $"Zones Visited: {stats.ZonesVisited}", statsPos2, Color.LightGreen);
        spriteBatch.DrawString(_font, $"Biomes Found: {stats.BiomesDiscovered}", statsPos3, Color.Yellow);
        spriteBatch.DrawString(_font, $"Journal Entries: {stats.TotalEntries}", statsPos4, Color.Orange);
    }

    private void DrawEntries(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixelTexture, _contentArea, new Color(45, 40, 65));
        
        int entryHeight = 55;
        int startIndex = Math.Max(0, _scrollOffset);
        int endIndex = Math.Min(_displayedEntries.Count, startIndex + _maxVisibleEntries);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            var entry = _displayedEntries[i];
            int yOffset = (i - startIndex) * entryHeight;
            
            Rectangle entryRect = new Rectangle(
                _contentArea.X + 5,
                _contentArea.Y + 5 + yOffset,
                _contentArea.Width - 10,
                entryHeight - 5
            );
            
            // Alternate background colors
            Color entryBg = i % 2 == 0 ? new Color(55, 50, 75) : new Color(50, 45, 70);
            spriteBatch.Draw(_pixelTexture, entryRect, entryBg);
            
            // Draw entry content
            Vector2 titlePos = new Vector2(entryRect.X + 5, entryRect.Y + 2);
            Vector2 timePos = new Vector2(entryRect.Right - 120, entryRect.Y + 2);
            Vector2 descPos = new Vector2(entryRect.X + 5, entryRect.Y + 18);
            
            Color titleColor = GetEntryTypeColor(entry.Type);
            spriteBatch.DrawString(_font, entry.Title, titlePos, titleColor);
            spriteBatch.DrawString(_font, $"Day {entry.GameDay} - {entry.GameTime}", timePos, Color.LightGray);
            
            // Wrap description text
            string wrappedDesc = WrapText(entry.Description, _contentArea.Width - 20);
            spriteBatch.DrawString(_font, wrappedDesc, descPos, Color.White);
        }
        
        // Draw scroll indicator
        if (_displayedEntries.Count > _maxVisibleEntries)
        {
            float scrollPercent = (float)_scrollOffset / (_displayedEntries.Count - _maxVisibleEntries);
            int scrollBarHeight = _contentArea.Height - 20;
            int scrollThumbHeight = 20;
            int scrollThumbY = (int)(scrollPercent * (scrollBarHeight - scrollThumbHeight));
            
            Rectangle scrollBar = new Rectangle(_contentArea.Right - 15, _contentArea.Y + 10, 5, scrollBarHeight);
            Rectangle scrollThumb = new Rectangle(_contentArea.Right - 15, _contentArea.Y + 10 + scrollThumbY, 5, scrollThumbHeight);
            
            spriteBatch.Draw(_pixelTexture, scrollBar, Color.DarkGray);
            spriteBatch.Draw(_pixelTexture, scrollThumb, Color.LightGray);
        }
    }

    private Color GetEntryTypeColor(JournalEntryType type)
    {
        return type switch
        {
            JournalEntryType.GameStart => Color.Gold,
            JournalEntryType.ZoneDiscovery => Color.LightGreen,
            JournalEntryType.BiomeDiscovery => Color.Cyan,
            JournalEntryType.WeatherEvent => Color.LightBlue,
            JournalEntryType.Milestone => Color.Orange,
            JournalEntryType.SpecialEvent => Color.Magenta,
            _ => Color.White
        };
    }

    private string WrapText(string text, int maxWidth)
    {
        if (_font == null) return text;
        
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";
        
        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            
            if (_font.MeasureString(testLine).X <= maxWidth)
            {
                currentLine = testLine;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
        }
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }
        
        return string.Join("\n", lines.Take(2)); // Limit to 2 lines per entry
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

    private void RefreshEntries()
    {
        _displayedEntries = _journalManager.GetRecentEntries(50); // Show last 50 entries
        _scrollOffset = 0;
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}