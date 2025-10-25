using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace HiddenHorizons;

public class EscapeMenu
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private bool _isVisible;
    private Rectangle _menuArea;
    private Rectangle[] _buttonAreas;
    private string[] _buttonTexts;
    private int _hoveredButton = -1;
    private int _screenWidth;
    private int _screenHeight;
    
    // Menu options
    private const int RETURN_TO_GAME = 0;
    private const int RETURN_TO_START = 1;
    private const int OPTIONS = 2;
    private const int EXIT_DESKTOP = 3;
    
    // Submenu state
    private bool _showingOptionsMenu;
    private bool _showingAudioOptions;
    private Rectangle[] _optionsButtonAreas;
    private string[] _optionsButtonTexts;
    private int _hoveredOptionsButton = -1;
    
    // Audio options
    private AudioManager _audioManager;
    private Rectangle[] _audioSliderAreas;
    private Rectangle[] _audioSliderHandles;
    private string[] _audioSliderLabels;
    private bool _draggingSlider = false;
    private int _draggedSliderIndex = -1;
    
    // Temporary audio settings (before Apply)
    private float _tempMasterVolume;
    private float _tempMusicVolume;
    private float _tempSfxVolume;
    private float _tempAmbientVolume;
    private bool _hasUnappliedChanges = false;
    
    // Applied settings (to revert to if user cancels)
    private float _appliedMasterVolume;
    private float _appliedMusicVolume;
    private float _appliedSfxVolume;
    private float _appliedAmbientVolume;
    
    // Options submenu
    private const int GAME_OPTIONS = 0;
    private const int AUDIO_OPTIONS = 1;
    private const int DISPLAY_OPTIONS = 2;
    private const int BACK_TO_MAIN = 3;

    public EscapeMenu(GraphicsDevice graphicsDevice)
    {
        // Create pixel texture for backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Initialize button texts
        _buttonTexts = new string[]
        {
            "Return to Game",
            "Return to Start Menu",
            "Options",
            "Exit to Desktop"
        };
        
        _optionsButtonTexts = new string[]
        {
            "Game Options",
            "Audio Options", 
            "Display Options",
            "Back"
        };
        
        _audioSliderLabels = new string[]
        {
            "Master Volume",
            "Music Volume",
            "Sound Effects",
            "Ambient Sounds"
        };
        
        _isVisible = false;
        _showingOptionsMenu = false;
        _showingAudioOptions = false;
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }
    
    public void SetAudioManager(AudioManager audioManager)
    {
        _audioManager = audioManager;
    }
    
    private void ApplyAudioSettings()
    {
        if (_audioManager == null) return;
        
        // Save current settings as applied settings
        _appliedMasterVolume = _audioManager.MasterVolume;
        _appliedMusicVolume = _audioManager.MusicVolume;
        _appliedSfxVolume = _audioManager.SfxVolume;
        _appliedAmbientVolume = _audioManager.AmbientVolume;
        
        _hasUnappliedChanges = false;
    }
    
    private void ResetAudioSettings()
    {
        if (_audioManager == null) return;
        
        // Reset to AudioManager defaults and apply immediately for preview
        _tempMasterVolume = AudioManager.DEFAULT_MASTER_VOLUME;
        _tempMusicVolume = AudioManager.DEFAULT_MUSIC_VOLUME;
        _tempSfxVolume = AudioManager.DEFAULT_SFX_VOLUME;
        _tempAmbientVolume = AudioManager.DEFAULT_AMBIENT_VOLUME;
        
        _audioManager.SetMasterVolume(_tempMasterVolume);
        _audioManager.SetMusicVolume(_tempMusicVolume);
        _audioManager.SetSfxVolume(_tempSfxVolume);
        _audioManager.SetAmbientVolume(_tempAmbientVolume);
        
        _hasUnappliedChanges = true;
    }
    
    private void RevertAudioSettings()
    {
        if (_audioManager == null) return;
        
        // Revert to last applied settings
        _audioManager.SetMasterVolume(_appliedMasterVolume);
        _audioManager.SetMusicVolume(_appliedMusicVolume);
        _audioManager.SetSfxVolume(_appliedSfxVolume);
        _audioManager.SetAmbientVolume(_appliedAmbientVolume);
        

    }

    public void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        // Center the menu on screen
        int menuWidth = 300;
        int menuHeight = 250;
        _menuArea = new Rectangle(
            (screenWidth - menuWidth) / 2,
            (screenHeight - menuHeight) / 2,
            menuWidth,
            menuHeight
        );
        
        // Create button areas
        _buttonAreas = new Rectangle[4];
        _optionsButtonAreas = new Rectangle[4];
        _audioSliderAreas = new Rectangle[4];
        _audioSliderHandles = new Rectangle[4];
        
        int buttonWidth = 250;
        int buttonHeight = 40;
        int buttonSpacing = 10;
        int startY = _menuArea.Y + 40;
        
        for (int i = 0; i < 4; i++)
        {
            _buttonAreas[i] = new Rectangle(
                _menuArea.X + (menuWidth - buttonWidth) / 2,
                startY + i * (buttonHeight + buttonSpacing),
                buttonWidth,
                buttonHeight
            );
            
            // Options menu buttons (same layout)
            _optionsButtonAreas[i] = new Rectangle(
                _menuArea.X + (menuWidth - buttonWidth) / 2,
                startY + i * (buttonHeight + buttonSpacing),
                buttonWidth,
                buttonHeight
            );
            
            // Audio sliders - better spacing and positioning
            int sliderWidth = 180;
            int sliderHeight = 16;
            int sliderSpacing = 45; // More space between sliders
            int sliderY = startY + i * sliderSpacing + 15;
            
            _audioSliderAreas[i] = new Rectangle(
                _menuArea.X + 30, // Left-aligned with some margin
                sliderY,
                sliderWidth,
                sliderHeight
            );
            
            // Initialize slider handles (will be updated based on volume values)
            _audioSliderHandles[i] = new Rectangle(
                _audioSliderAreas[i].X,
                _audioSliderAreas[i].Y - 3,
                12,
                sliderHeight + 6
            );
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        // Handle mouse hover and slider dragging
        var mouseState = Mouse.GetState();
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        
        _hoveredButton = -1;
        _hoveredOptionsButton = -1;
        
        if (_showingAudioOptions)
        {
            UpdateAudioSliders(mouseState, mousePos);
        }
        else if (_showingOptionsMenu)
        {
            for (int i = 0; i < _optionsButtonAreas.Length; i++)
            {
                if (_optionsButtonAreas[i].Contains(mousePos))
                {
                    _hoveredOptionsButton = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < _buttonAreas.Length; i++)
            {
                if (_buttonAreas[i].Contains(mousePos))
                {
                    _hoveredButton = i;
                    break;
                }
            }
        }
    }
    
    private void UpdateAudioSliders(MouseState mouseState, Vector2 mousePos)
    {
        if (_audioManager == null) return;
        
        // Update slider handle positions based on current AudioManager values (for real-time preview)
        float[] volumes = {
            _audioManager.MasterVolume,
            _audioManager.MusicVolume,
            _audioManager.SfxVolume,
            _audioManager.AmbientVolume
        };
        
        for (int i = 0; i < _audioSliderAreas.Length; i++)
        {
            var sliderArea = _audioSliderAreas[i];
            int handleWidth = 12;
            int handleX = (int)(sliderArea.X + volumes[i] * (sliderArea.Width - handleWidth));
            _audioSliderHandles[i] = new Rectangle(handleX, sliderArea.Y - 3, handleWidth, sliderArea.Height + 6);
        }
        
        // Handle slider dragging
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            if (!_draggingSlider)
            {
                // Check if starting to drag a slider
                for (int i = 0; i < _audioSliderAreas.Length; i++)
                {
                    if (_audioSliderAreas[i].Contains(mousePos))
                    {
                        _draggingSlider = true;
                        _draggedSliderIndex = i;
                        break;
                    }
                }
            }
            
            // Update dragged slider (apply immediately for preview)
            if (_draggingSlider && _draggedSliderIndex >= 0)
            {
                var sliderArea = _audioSliderAreas[_draggedSliderIndex];
                float normalizedValue = Math.Max(0f, Math.Min(1f, (mousePos.X - sliderArea.X) / (float)sliderArea.Width));
                
                // Update both temporary values and AudioManager for immediate preview
                switch (_draggedSliderIndex)
                {
                    case 0: 
                        _tempMasterVolume = normalizedValue;
                        _audioManager.SetMasterVolume(normalizedValue);
                        break;
                    case 1: 
                        _tempMusicVolume = normalizedValue;
                        _audioManager.SetMusicVolume(normalizedValue);
                        break;
                    case 2: 
                        _tempSfxVolume = normalizedValue;
                        _audioManager.SetSfxVolume(normalizedValue);
                        break;
                    case 3: 
                        _tempAmbientVolume = normalizedValue;
                        _audioManager.SetAmbientVolume(normalizedValue);
                        break;
                }
                _hasUnappliedChanges = true;
            }
        }
        else
        {
            _draggingSlider = false;
            _draggedSliderIndex = -1;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isVisible) return;
        
        // Draw semi-transparent overlay
        Rectangle fullScreen = new Rectangle(0, 0, _screenWidth, _screenHeight);
        spriteBatch.Draw(_pixelTexture, fullScreen, Color.Black * 0.7f);
        
        // Draw menu background
        spriteBatch.Draw(_pixelTexture, _menuArea, Color.Black * 0.9f);
        DrawBorder(spriteBatch, _menuArea, Color.White, 2);
        
        if (_font != null)
        {
            // Draw title
            string title = _showingAudioOptions ? "Audio Options" : 
                          _showingOptionsMenu ? "Options" : "Game Menu";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                _menuArea.X + (_menuArea.Width - titleSize.X) / 2,
                _menuArea.Y + 10
            );
            spriteBatch.DrawString(_font, title, titlePos, Color.White);
            
            // Draw appropriate content
            if (_showingAudioOptions)
            {
                DrawAudioOptions(spriteBatch);
            }
            else if (_showingOptionsMenu)
            {
                DrawOptionsButtons(spriteBatch);
            }
            else
            {
                DrawMainButtons(spriteBatch);
            }
        }
    }

    private void DrawMainButtons(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _buttonAreas.Length; i++)
        {
            // Button background
            Color buttonColor = _hoveredButton == i ? Color.Gray * 0.8f : Color.DarkGray * 0.6f;
            spriteBatch.Draw(_pixelTexture, _buttonAreas[i], buttonColor);
            
            // Button border
            Color borderColor = _hoveredButton == i ? Color.White : Color.Gray;
            DrawBorder(spriteBatch, _buttonAreas[i], borderColor, 1);
            
            // Button text
            Vector2 textSize = _font.MeasureString(_buttonTexts[i]);
            Vector2 textPos = new Vector2(
                _buttonAreas[i].X + (_buttonAreas[i].Width - textSize.X) / 2,
                _buttonAreas[i].Y + (_buttonAreas[i].Height - textSize.Y) / 2
            );
            
            Color textColor = _hoveredButton == i ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, _buttonTexts[i], textPos, textColor);
        }
    }

    private void DrawOptionsButtons(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _optionsButtonAreas.Length; i++)
        {
            // Button background
            Color buttonColor = _hoveredOptionsButton == i ? Color.Gray * 0.8f : Color.DarkGray * 0.6f;
            spriteBatch.Draw(_pixelTexture, _optionsButtonAreas[i], buttonColor);
            
            // Button border
            Color borderColor = _hoveredOptionsButton == i ? Color.White : Color.Gray;
            DrawBorder(spriteBatch, _optionsButtonAreas[i], borderColor, 1);
            
            // Button text
            Vector2 textSize = _font.MeasureString(_optionsButtonTexts[i]);
            Vector2 textPos = new Vector2(
                _optionsButtonAreas[i].X + (_optionsButtonAreas[i].Width - textSize.X) / 2,
                _optionsButtonAreas[i].Y + (_optionsButtonAreas[i].Height - textSize.Y) / 2
            );
            
            Color textColor = _hoveredOptionsButton == i ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, _optionsButtonTexts[i], textPos, textColor);
        }
    }

    private void DrawAudioOptions(SpriteBatch spriteBatch)
    {
        if (_audioManager == null) 
        {
            // Draw "Audio Manager not available" message
            Vector2 errorPos = new Vector2(_menuArea.X + 20, _menuArea.Y + 50);
            spriteBatch.DrawString(_font, "Audio Manager not available", errorPos, Color.Red);
            return;
        }
        
        float[] volumes = {
            _audioManager.MasterVolume,
            _audioManager.MusicVolume,
            _audioManager.SfxVolume,
            _audioManager.AmbientVolume
        };
        
        for (int i = 0; i < _audioSliderAreas.Length; i++)
        {
            // Draw label
            Vector2 labelPos = new Vector2(
                _audioSliderAreas[i].X,
                _audioSliderAreas[i].Y - 20
            );
            spriteBatch.DrawString(_font, _audioSliderLabels[i], labelPos, Color.White);
            
            // Draw slider track background
            spriteBatch.Draw(_pixelTexture, _audioSliderAreas[i], Color.DarkGray * 0.8f);
            
            // Draw filled portion of slider (visual feedback)
            int filledWidth = (int)(_audioSliderAreas[i].Width * volumes[i]);
            if (filledWidth > 0)
            {
                Rectangle filledArea = new Rectangle(
                    _audioSliderAreas[i].X,
                    _audioSliderAreas[i].Y,
                    filledWidth,
                    _audioSliderAreas[i].Height
                );
                spriteBatch.Draw(_pixelTexture, filledArea, Color.CornflowerBlue * 0.7f);
            }
            
            // Draw slider border
            DrawBorder(spriteBatch, _audioSliderAreas[i], Color.Gray, 1);
            
            // Draw slider handle
            Color handleColor = (_draggingSlider && _draggedSliderIndex == i) ? Color.Yellow : Color.White;
            spriteBatch.Draw(_pixelTexture, _audioSliderHandles[i], handleColor);
            DrawBorder(spriteBatch, _audioSliderHandles[i], Color.Black, 1);
            
            // Draw volume percentage
            string volumeText = $"{(volumes[i] * 100):F0}%";
            Vector2 volumeSize = _font.MeasureString(volumeText);
            Vector2 volumePos = new Vector2(
                _audioSliderAreas[i].Right + 15,
                _audioSliderAreas[i].Y + (_audioSliderAreas[i].Height - volumeSize.Y) / 2
            );
            spriteBatch.DrawString(_font, volumeText, volumePos, Color.LightGray);
        }
        
        // Draw buttons at bottom (with more spacing from sliders)
        int buttonY = _menuArea.Bottom - 35;
        int buttonHeight = 25;
        int buttonSpacing = 10;
        
        // Apply button
        Rectangle applyButton = new Rectangle(_menuArea.X + 20, buttonY, 80, buttonHeight);
        Color applyColor = _hasUnappliedChanges ? Color.Green * 0.7f : Color.DarkGray * 0.5f;
        spriteBatch.Draw(_pixelTexture, applyButton, applyColor);
        DrawBorder(spriteBatch, applyButton, _hasUnappliedChanges ? Color.Green : Color.Gray, 1);
        
        Vector2 applyTextSize = _font.MeasureString("Apply");
        Vector2 applyTextPos = new Vector2(
            applyButton.X + (applyButton.Width - applyTextSize.X) / 2,
            applyButton.Y + (applyButton.Height - applyTextSize.Y) / 2
        );
        Color applyTextColor = _hasUnappliedChanges ? Color.White : Color.Gray;
        spriteBatch.DrawString(_font, "Apply", applyTextPos, applyTextColor);
        
        // Reset button
        Rectangle resetButton = new Rectangle(applyButton.Right + buttonSpacing, buttonY, 80, buttonHeight);
        spriteBatch.Draw(_pixelTexture, resetButton, Color.DarkRed * 0.6f);
        DrawBorder(spriteBatch, resetButton, Color.Red, 1);
        
        Vector2 resetTextSize = _font.MeasureString("Reset");
        Vector2 resetTextPos = new Vector2(
            resetButton.X + (resetButton.Width - resetTextSize.X) / 2,
            resetButton.Y + (resetButton.Height - resetTextSize.Y) / 2
        );
        spriteBatch.DrawString(_font, "Reset", resetTextPos, Color.White);
        
        // Back button
        Rectangle backButton = new Rectangle(resetButton.Right + buttonSpacing, buttonY, 80, buttonHeight);
        spriteBatch.Draw(_pixelTexture, backButton, Color.DarkGray * 0.6f);
        DrawBorder(spriteBatch, backButton, Color.Gray, 1);
        
        Vector2 backTextSize = _font.MeasureString("Back");
        Vector2 backTextPos = new Vector2(
            backButton.X + (backButton.Width - backTextSize.X) / 2,
            backButton.Y + (backButton.Height - backTextSize.Y) / 2
        );
        spriteBatch.DrawString(_font, "Back", backTextPos, Color.White);
        
        // Show warning if there are unapplied changes
        if (_hasUnappliedChanges)
        {
            string warningText = "Changes not applied - click Apply to save";
            Vector2 warningSize = _font.MeasureString(warningText);
            Vector2 warningPos = new Vector2(
                _menuArea.X + (_menuArea.Width - warningSize.X) / 2,
                buttonY - 20
            );
            spriteBatch.DrawString(_font, warningText, warningPos, Color.Yellow);
        }
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

    public bool HandleMouseClick(Vector2 mousePosition)
    {
        if (!_isVisible) return false;
        
        if (_showingAudioOptions)
        {
            int buttonY = _menuArea.Bottom - 35;
            int buttonHeight = 25;
            int buttonSpacing = 10;
            
            // Apply button
            Rectangle applyButton = new Rectangle(_menuArea.X + 20, buttonY, 80, buttonHeight);
            if (applyButton.Contains(mousePosition) && _hasUnappliedChanges)
            {
                ApplyAudioSettings();
                return true;
            }
            
            // Reset button
            Rectangle resetButton = new Rectangle(applyButton.Right + buttonSpacing, buttonY, 80, buttonHeight);
            if (resetButton.Contains(mousePosition))
            {
                ResetAudioSettings();
                return true;
            }
            
            // Back button
            Rectangle backButton = new Rectangle(resetButton.Right + buttonSpacing, buttonY, 80, buttonHeight);
            if (backButton.Contains(mousePosition))
            {
                // Revert to applied settings if there are unapplied changes
                if (_hasUnappliedChanges)
                {
                    RevertAudioSettings();
                }
                _showingAudioOptions = false;
                _showingOptionsMenu = true;
                return true;
            }
            
            // Check slider clicks for immediate response
            if (_audioManager != null)
            {
                for (int i = 0; i < _audioSliderAreas.Length; i++)
                {
                    if (_audioSliderAreas[i].Contains(mousePosition))
                    {
                        var sliderArea = _audioSliderAreas[i];
                        float normalizedValue = Math.Max(0f, Math.Min(1f, (mousePosition.X - sliderArea.X) / (float)sliderArea.Width));
                        
                        // Update both temporary values and AudioManager for immediate preview
                        switch (i)
                        {
                            case 0: 
                                _tempMasterVolume = normalizedValue;
                                _audioManager.SetMasterVolume(normalizedValue);
                                break;
                            case 1: 
                                _tempMusicVolume = normalizedValue;
                                _audioManager.SetMusicVolume(normalizedValue);
                                break;
                            case 2: 
                                _tempSfxVolume = normalizedValue;
                                _audioManager.SetSfxVolume(normalizedValue);
                                break;
                            case 3: 
                                _tempAmbientVolume = normalizedValue;
                                _audioManager.SetAmbientVolume(normalizedValue);
                                break;
                        }
                        _hasUnappliedChanges = true;
                        return true;
                    }
                }
            }
            
            return true;
        }
        else if (_showingOptionsMenu)
        {
            for (int i = 0; i < _optionsButtonAreas.Length; i++)
            {
                if (_optionsButtonAreas[i].Contains(mousePosition))
                {
                    HandleOptionsButtonClick(i);
                    return true;
                }
            }
        }
        else
        {
            for (int i = 0; i < _buttonAreas.Length; i++)
            {
                if (_buttonAreas[i].Contains(mousePosition))
                {
                    HandleMainButtonClick(i);
                    return true;
                }
            }
        }
        
        return false;
    }

    private void HandleMainButtonClick(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case RETURN_TO_GAME:
                Hide();
                break;
                
            case RETURN_TO_START:
                // Placeholder - would need to implement start menu system
                break;
                
            case OPTIONS:
                _showingOptionsMenu = true;
                break;
                
            case EXIT_DESKTOP:
                // This will be handled by the Game1 class
                OnExitRequested?.Invoke();
                break;
        }
    }

    private void HandleOptionsButtonClick(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case GAME_OPTIONS:
                // Placeholder for game options
                break;
                
            case AUDIO_OPTIONS:
                // Load current values into temporary and applied settings
                if (_audioManager != null)
                {
                    _tempMasterVolume = _appliedMasterVolume = _audioManager.MasterVolume;
                    _tempMusicVolume = _appliedMusicVolume = _audioManager.MusicVolume;
                    _tempSfxVolume = _appliedSfxVolume = _audioManager.SfxVolume;
                    _tempAmbientVolume = _appliedAmbientVolume = _audioManager.AmbientVolume;
                    _hasUnappliedChanges = false;
                }
                _showingAudioOptions = true;
                _showingOptionsMenu = false;
                break;
                
            case DISPLAY_OPTIONS:
                // Placeholder for display options
                break;
                
            case BACK_TO_MAIN:
                _showingOptionsMenu = false;
                break;
        }
    }

    public void Show()
    {
        _isVisible = true;
        _showingOptionsMenu = false;
        _showingAudioOptions = false;
        _hoveredButton = -1;
        _hoveredOptionsButton = -1;
        _draggingSlider = false;
        _draggedSliderIndex = -1;
    }

    public void Hide()
    {
        _isVisible = false;
        _showingOptionsMenu = false;
        _showingAudioOptions = false;
        _draggingSlider = false;
        _draggedSliderIndex = -1;
    }

    public void Toggle()
    {
        if (_isVisible)
            Hide();
        else
            Show();
    }

    public bool IsVisible => _isVisible;

    // Event for when exit is requested
    public event Action OnExitRequested;

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}