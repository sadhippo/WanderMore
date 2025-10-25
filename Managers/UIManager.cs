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
    private EscapeMenu _escapeMenu;
    private InventoryUI _inventoryUI;
    private Rectangle _clockArea;
    private Rectangle _dayCounterArea;
    private Rectangle _zoneNameArea;
    private Rectangle _weatherArea;
    private Rectangle _pauseButtonArea;
    private Rectangle _muteButtonArea;
    private Rectangle _inventoryButtonArea;
    
    // Pause system
    private bool _isPaused;
    
    // Audio system
    private AudioManager _audioManager;
    private bool _isMuted;
    
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
        
        // Initialize escape menu
        _escapeMenu = new EscapeMenu(graphicsDevice);
        
        // Initialize inventory UI
        _inventoryUI = new InventoryUI(graphicsDevice);
        
        // Create pixel texture for UI backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Define UI areas (top-left corner)
        _clockArea = new Rectangle(10, 10, 200, 40);
        _dayCounterArea = new Rectangle(10, 55, 200, 30);
        _zoneNameArea = new Rectangle(10, 90, 250, 35);
        _weatherArea = new Rectangle(10, 130, 220, 30);
        _pauseButtonArea = new Rectangle(220, 10, 40, 40); // Next to clock
        _muteButtonArea = new Rectangle(220, 55, 60, 30); // Next to day counter
        _inventoryButtonArea = new Rectangle(290, 10, 60, 30); // Next to pause button
        
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
        _escapeMenu?.LoadContent(font);
        _inventoryUI?.LoadContent(font);
    }
    
    public void SetAudioManager(AudioManager audioManager)
    {
        _audioManager = audioManager;
        _escapeMenu?.SetAudioManager(audioManager);
    }
    
    public void SetInventoryManager(InventoryManager inventoryManager)
    {
        _inventoryUI?.SetInventoryManager(inventoryManager);
    }

    public void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        // Update ticker area to match screen size
        _tickerArea = new Rectangle(50, screenHeight - 70, screenWidth - 100, 50);
        
        // Update escape menu screen size
        _escapeMenu?.UpdateScreenSize(screenWidth, screenHeight);
        
        // Update inventory UI screen size
        _inventoryUI?.UpdateScreenSize(screenWidth, screenHeight);
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
        
        // Update escape menu
        _escapeMenu?.Update(gameTime);
        
        // Update inventory UI
        _inventoryUI?.Update(gameTime);
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
            
            // Draw mute button background
            Color muteBackgroundColor = _isMuted ? new Color(150, 50, 50, 150) : new Color(0, 0, 0, 120);
            DrawUIPanel(spriteBatch, _muteButtonArea, muteBackgroundColor);
            
            // Draw inventory button background
            Color inventoryBackgroundColor = _inventoryUI?.IsVisible == true ? new Color(100, 150, 100, 150) : new Color(0, 0, 0, 120);
            DrawUIPanel(spriteBatch, _inventoryButtonArea, inventoryBackgroundColor);
            
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
        
        // Draw escape menu on top of everything else
        _escapeMenu?.Draw(spriteBatch);
        
        // Draw inventory UI on top of everything else
        _inventoryUI?.Draw(spriteBatch);
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
        
        // Draw mute button
        string muteText = _isMuted ? "MUTE" : "MUTE";
        Color muteTextColor = _isMuted ? Color.Red : Color.White;
        Vector2 muteTextPos = new Vector2(_muteButtonArea.X + 10, _muteButtonArea.Y + 8);
        spriteBatch.DrawString(_font, muteText, muteTextPos, muteTextColor);
        
        // Draw inventory button
        string inventoryText = "INV";
        Color inventoryTextColor = _inventoryUI?.IsVisible == true ? Color.Green : Color.White;
        Vector2 inventoryTextPos = new Vector2(_inventoryButtonArea.X + 15, _inventoryButtonArea.Y + 8);
        spriteBatch.DrawString(_font, inventoryText, inventoryTextPos, inventoryTextColor);
        
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
        
        // Draw mute button (fallback when no font)
        Color muteColor = _isMuted ? Color.Red : Color.Gray;
        spriteBatch.Draw(_pixelTexture, _muteButtonArea, muteColor);
        
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
        
        // Progress fill - calculate overall daily progress
        float overallProgress = GetOverallDayProgress();
        int fillWidth = (int)(progressArea.Width * overallProgress);
        Rectangle fillArea = new Rectangle(progressArea.X, progressArea.Y, fillWidth, progressArea.Height);
        
        Color progressColor = _timeManager.CurrentTimeOfDay switch
        {
            TimeOfDay.Dawn => Color.Orange,
            TimeOfDay.Day => Color.Yellow,
            TimeOfDay.Dusk => Color.Red,
            TimeOfDay.Night => Color.DarkBlue,
            _ => Color.White
        };
        
        spriteBatch.Draw(_pixelTexture, fillArea, progressColor);
    }

    private float GetOverallDayProgress()
    {
        // Calculate progress through the entire day cycle (0.0 to 1.0)
        return _timeManager.CurrentGameHour / 24f;
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
        // Check inventory UI first (highest priority when visible)
        if (_inventoryUI != null && _inventoryUI.HandleMouseClick(mousePosition))
        {
            return true;
        }
        
        // Check escape menu next
        if (_escapeMenu != null && _escapeMenu.HandleMouseClick(mousePosition))
        {
            return true;
        }
        
        // Check if pause button was clicked
        if (_pauseButtonArea.Contains(mousePosition))
        {
            _isPaused = !_isPaused;
            return true; // Consumed the click
        }
        
        // Check if mute button was clicked
        if (_muteButtonArea.Contains(mousePosition))
        {
            ToggleMute();
            return true; // Consumed the click
        }
        
        // Check if inventory button was clicked
        if (_inventoryButtonArea.Contains(mousePosition))
        {
            _inventoryUI?.Toggle();
            return true; // Consumed the click
        }
        
        return false; // Click not handled
    }

    public bool HandleMouseHover(Vector2 mousePosition, out string tooltip)
    {
        tooltip = null;
        
        // Check inventory UI hover first
        if (_inventoryUI != null && _inventoryUI.HandleMouseHover(mousePosition))
        {
            return true;
        }
        
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

    public void ShowEscapeMenu()
    {
        _escapeMenu?.Show();
    }

    public void HideEscapeMenu()
    {
        _escapeMenu?.Hide();
    }

    public void ToggleEscapeMenu()
    {
        _escapeMenu?.Toggle();
    }

    public bool IsEscapeMenuVisible => _escapeMenu?.IsVisible ?? false;

    public EscapeMenu EscapeMenu => _escapeMenu;
    
    public void ToggleInventory()
    {
        _inventoryUI?.Toggle();
    }
    
    public bool IsInventoryVisible => _inventoryUI?.IsVisible ?? false;
    
    private float _previousMasterVolume = 1.0f;
    
    public void ToggleMute()
    {
        if (_audioManager == null) return;
        
        _isMuted = !_isMuted;
        
        if (_isMuted)
        {
            // Store current volume and mute
            _previousMasterVolume = _audioManager.MasterVolume;
            _audioManager.SetMasterVolume(0.0f);
            System.Console.WriteLine("[UI] Audio muted");
        }
        else
        {
            // Restore previous volume
            _audioManager.SetMasterVolume(_previousMasterVolume);
            System.Console.WriteLine($"[UI] Audio unmuted (volume: {_previousMasterVolume:F2})");
        }
    }
    
    public bool IsMuted => _isMuted;

    public void Dispose()
    {
        // Unsubscribe from journal events
        if (_journalManager != null)
        {
            _journalManager.NewEntryAdded -= OnNewJournalEntry;
        }
        
        _pixelTexture?.Dispose();
        _statsHUD?.Dispose();
        _escapeMenu?.Dispose();
        _inventoryUI?.Dispose();
    }
}

// Extension class for easy time configuration
public static class TimeManagerExtensions
{
    public static void SetDayNightCycle(this TimeManager timeManager, float totalDayMinutes)
    {
        timeManager.SetDayLength(totalDayMinutes * 60f);
    }
}