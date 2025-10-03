using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;

namespace HiddenHorizons;

public class UIManager
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private TimeManager _timeManager;
    private ZoneManager _zoneManager;
    private WeatherManager _weatherManager;
    private StatsManager _statsManager;
    private JournalManager _journalManager;
    private StatsHUD _statsHUD;
    private Rectangle _clockArea;
    private Rectangle _dayCounterArea;
    private Rectangle _zoneNameArea;
    private Rectangle _weatherArea;
    private Rectangle _pauseButtonArea;
    
    // Pause system
    private bool _isPaused;
    
    // Journal ticker system
    private string _currentTickerText;
    private float _tickerTimer;
    private float _tickerAlpha;
    private const float TICKER_DISPLAY_TIME = 5.0f; // Show for 5 seconds
    private const float TICKER_FADE_TIME = 1.0f; // Fade out over 1 second
    private Rectangle _tickerArea;
    private Texture2D _uiButtonsTexture;
    private Rectangle _pauseButtonSource;

    public UIManager(GraphicsDevice graphicsDevice, TimeManager timeManager, ZoneManager zoneManager, WeatherManager weatherManager, StatsManager statsManager, JournalManager journalManager = null)
    {
        _timeManager = timeManager;
        _zoneManager = zoneManager;
        _weatherManager = weatherManager;
        _statsManager = statsManager;
        _journalManager = journalManager;
        
        // Subscribe to journal events if available
        if (_journalManager != null)
        {
            _journalManager.NewEntryAdded += OnNewJournalEntry;
        }
        
        // Initialize stats HUD
        _statsHUD = new StatsHUD(graphicsDevice);
        
        // Create pixel texture for UI backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Define UI areas (top-left corner)
        _clockArea = new Rectangle(10, 10, 200, 40);
        _dayCounterArea = new Rectangle(10, 55, 200, 30);
        _zoneNameArea = new Rectangle(10, 90, 250, 35);
        _weatherArea = new Rectangle(10, 130, 220, 30);
        _pauseButtonArea = new Rectangle(220, 10, 40, 40); // Next to clock
        
        // Define pause button sprite source (full image)
        _pauseButtonSource = new Rectangle(0, 0, 32, 32);
        _isPaused = false;
        
        // Initialize ticker (will be positioned at bottom of screen)
        // We'll update this in LoadContent when we have access to screen dimensions
        _tickerArea = new Rectangle(50, 700, 924, 50);
        _currentTickerText = "";
        _tickerTimer = 0f;
        _tickerAlpha = 0f;
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
        _statsHUD?.LoadContent(font);
    }

    public void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        // Update ticker area to match screen size
        _tickerArea = new Rectangle(50, screenHeight - 70, screenWidth - 100, 50);
    }

    public void LoadUITextures(ContentManager content)
    {
        try
        {
            _uiButtonsTexture = content.Load<Texture2D>("ui/pausebutton");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load pause button: {ex.Message}");
        }
    }

    public void Update(GameTime gameTime)
    {
        // Update ticker timer
        if (_tickerTimer > 0f)
        {
            _tickerTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Calculate alpha based on remaining time
            if (_tickerTimer <= TICKER_FADE_TIME)
            {
                _tickerAlpha = _tickerTimer / TICKER_FADE_TIME;
            }
            else
            {
                _tickerAlpha = 1.0f;
            }
            
            // Clear ticker when time is up
            if (_tickerTimer <= 0f)
            {
                _currentTickerText = "";
                _tickerAlpha = 0f;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (spriteBatch == null || _pixelTexture == null)
            return;
            
        try
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
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error in UIManager.Draw: {ex.Message}");
            // Continue without crashing
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
        
        // Draw pause button
        if (_uiButtonsTexture != null)
        {
            Color buttonColor = _isPaused ? Color.Red : Color.White;
            spriteBatch.Draw(_uiButtonsTexture, _pauseButtonArea, buttonColor);
        }
        
        // Draw pause indicator if paused
        if (_isPaused)
        {
            Vector2 pausedTextPos = new Vector2(_pauseButtonArea.X + 50, _pauseButtonArea.Y + 12);
            spriteBatch.DrawString(_font, "PAUSED", pausedTextPos, Color.Yellow);
        }
        
        // Draw stats HUD
        try
        {
            if (_statsManager != null && _statsHUD != null)
            {
                _statsHUD.Draw(spriteBatch, _statsManager.CurrentStats);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error drawing StatsHUD: {ex.Message}");
        }
        
        // Draw journal ticker
        try
        {
            DrawJournalTicker(spriteBatch);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error drawing journal ticker: {ex.Message}");
        }
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
        
        // Draw pause button (fallback when no texture)
        if (_uiButtonsTexture != null)
        {
            Color buttonColor = _isPaused ? Color.Red : Color.White;
            spriteBatch.Draw(_uiButtonsTexture, _pauseButtonArea, buttonColor);
        }
        else
        {
            Color pauseColor = _isPaused ? Color.Red : Color.Green;
            spriteBatch.Draw(_pixelTexture, _pauseButtonArea, pauseColor);
        }
        
        // Draw stats HUD (fallback mode)
        if (_statsManager != null && _statsHUD != null)
        {
            _statsHUD.Draw(spriteBatch, _statsManager.CurrentStats);
        }
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

    private void DrawJournalTicker(SpriteBatch spriteBatch)
    {
        if (string.IsNullOrEmpty(_currentTickerText) || _tickerAlpha <= 0f || _pixelTexture == null)
            return;

        // Draw ticker background with fade
        Color backgroundColor = new Color(0, 0, 0, (int)(180 * _tickerAlpha));
        spriteBatch.Draw(_pixelTexture, _tickerArea, backgroundColor);
        
        // Draw border with fade
        Color borderColor = new Color(255, 255, 255, (int)(128 * _tickerAlpha));
        DrawBorder(spriteBatch, _tickerArea, borderColor, 2);
        
        if (_font != null)
        {
            // Calculate text position (centered in ticker area)
            Vector2 textSize = _font.MeasureString(_currentTickerText);
            Vector2 textPosition = new Vector2(
                _tickerArea.X + (_tickerArea.Width - textSize.X) / 2,
                _tickerArea.Y + (_tickerArea.Height - textSize.Y) / 2
            );
            
            // Draw text with fade
            Color textColor = new Color(255, 255, 255, (int)(255 * _tickerAlpha));
            spriteBatch.DrawString(_font, _currentTickerText, textPosition, textColor);
        }
        else
        {
            // Fallback: draw a simple colored indicator
            Rectangle indicatorRect = new Rectangle(
                _tickerArea.X + 10, 
                _tickerArea.Y + 15, 
                _tickerArea.Width - 20, 
                20
            );
            Color indicatorColor = new Color(100, 200, 255, (int)(255 * _tickerAlpha));
            spriteBatch.Draw(_pixelTexture, indicatorRect, indicatorColor);
        }
    }

    public bool HandleMouseClick(Vector2 mousePosition)
    {
        // Check if pause button was clicked
        if (_pauseButtonArea.Contains(mousePosition))
        {
            _isPaused = !_isPaused;
            System.Console.WriteLine(_isPaused ? "Game paused" : "Game resumed");
            return true; // Consumed the click
        }
        
        return false; // Click not handled
    }

    public bool HandleMouseHover(Vector2 mousePosition, out string tooltip)
    {
        tooltip = null;
        
        // Check stats HUD hover
        if (_statsManager != null && _statsHUD != null)
        {
            return _statsHUD.HandleMouseHover(mousePosition, _statsManager.CurrentStats, out tooltip);
        }
        
        return false;
    }

    public bool IsPaused => _isPaused;

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        System.Console.WriteLine(_isPaused ? "Game paused" : "Game resumed");
    }

    public void ShowJournalEntry(string title, string description)
    {
        // Format the ticker text (removed emoji that causes font issues)
        _currentTickerText = $"Journal: {title} - {description}";
        
        // Truncate if too long
        if (_font != null && _currentTickerText.Length > 80)
        {
            _currentTickerText = _currentTickerText.Substring(0, 77) + "...";
        }
        
        // Reset timer and alpha
        _tickerTimer = TICKER_DISPLAY_TIME;
        _tickerAlpha = 1.0f;
    }

    private void OnNewJournalEntry(JournalEntry entry)
    {
        ShowJournalEntry(entry.Title, entry.Description);
    }

    public void Dispose()
    {
        // Unsubscribe from journal events
        if (_journalManager != null)
        {
            _journalManager.NewEntryAdded -= OnNewJournalEntry;
        }
        
        _pixelTexture?.Dispose();
        _statsHUD?.Dispose();
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