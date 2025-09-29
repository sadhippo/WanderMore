using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Handles the escape menu UI with pause functionality and navigation options
    /// </summary>
    public class EscapeMenuUI
    {
        private SpriteFont _font;
        private Texture2D _pixelTexture;
        private bool _isVisible;
        private bool _isPaused;
        private Rectangle _menuArea;
        private Rectangle _titleArea;
        
        // Menu options
        private readonly string[] _menuOptions = {
            "Resume Game",
            "Save Game", 
            "Load Game",
            "Options",
            "Exit Game"
        };
        
        private int _selectedIndex;
        private Rectangle[] _optionRectangles;
        
        // Input handling
        private KeyboardState _previousKeyboardState;
        
        // Events for menu actions
        public event EventHandler ResumeRequested;
        public event EventHandler SaveRequested;
        public event EventHandler LoadRequested;
        public event EventHandler OptionsRequested;
        public event EventHandler MainMenuRequested;
        
        // Pause function delegate
        private readonly Action<bool> _setPauseFunction;

        public bool IsVisible => _isVisible;
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Initializes a new instance of the EscapeMenuUI
        /// </summary>
        /// <param name="graphicsDevice">Graphics device for creating textures</param>
        /// <param name="setPauseFunction">Function to call when pausing/unpausing the game</param>
        public EscapeMenuUI(GraphicsDevice graphicsDevice, Action<bool> setPauseFunction)
        {
            _setPauseFunction = setPauseFunction ?? throw new ArgumentNullException(nameof(setPauseFunction));
            _selectedIndex = 0;
            
            // Create pixel texture for UI backgrounds
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
            // Define menu areas (centered on screen)
            int menuWidth = 400;
            int menuHeight = 350;
            int screenWidth = 1024; // Assuming default screen size
            int screenHeight = 768;
            
            _menuArea = new Rectangle(
                (screenWidth - menuWidth) / 2,
                (screenHeight - menuHeight) / 2,
                menuWidth,
                menuHeight
            );
            
            _titleArea = new Rectangle(_menuArea.X, _menuArea.Y, _menuArea.Width, 50);
            
            // Initialize option rectangles
            _optionRectangles = new Rectangle[_menuOptions.Length];
            int optionHeight = 40;
            int optionSpacing = 10;
            int startY = _titleArea.Bottom + 20;
            
            for (int i = 0; i < _menuOptions.Length; i++)
            {
                _optionRectangles[i] = new Rectangle(
                    _menuArea.X + 20,
                    startY + i * (optionHeight + optionSpacing),
                    _menuArea.Width - 40,
                    optionHeight
                );
            }
            
            _previousKeyboardState = Keyboard.GetState();
        }

        /// <summary>
        /// Loads content for the escape menu UI
        /// </summary>
        /// <param name="font">Font to use for text rendering</param>
        public void LoadContent(SpriteFont font)
        {
            _font = font;
        }

        /// <summary>
        /// Updates the escape menu UI, handling input and state changes
        /// </summary>
        /// <param name="gameTime">Game time information</param>
        public void Update(GameTime gameTime)
        {
            var currentKeyboardState = Keyboard.GetState();
            
            // Toggle menu with Escape key
            if (currentKeyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                ToggleMenu();
            }
            
            // Handle menu navigation when visible
            if (_isVisible)
            {
                // Navigate up
                if (currentKeyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
                {
                    _selectedIndex = (_selectedIndex - 1 + _menuOptions.Length) % _menuOptions.Length;
                }
                
                // Navigate down
                if (currentKeyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
                {
                    _selectedIndex = (_selectedIndex + 1) % _menuOptions.Length;
                }
                
                // Select option with Enter or Space
                if ((currentKeyboardState.IsKeyDown(Keys.Enter) || currentKeyboardState.IsKeyDown(Keys.Space)) && 
                    (!_previousKeyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Space)))
                {
                    SelectCurrentOption();
                }
            }
            
            _previousKeyboardState = currentKeyboardState;
        }

        /// <summary>
        /// Draws the escape menu UI
        /// </summary>
        /// <param name="spriteBatch">Sprite batch for rendering</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible) return;
            
            // Draw semi-transparent background overlay
            Rectangle screenRect = new Rectangle(0, 0, 1024, 768);
            spriteBatch.Draw(_pixelTexture, screenRect, Color.Black * 0.8f);
            
            // Draw menu background
            spriteBatch.Draw(_pixelTexture, _menuArea, new Color(40, 35, 60));
            DrawBorder(spriteBatch, _menuArea, Color.White, 3);
            
            // Draw title area
            spriteBatch.Draw(_pixelTexture, _titleArea, new Color(60, 50, 80));
            
            if (_font != null)
            {
                // Draw title
                string title = "Game Menu";
                Vector2 titleSize = _font.MeasureString(title);
                Vector2 titlePosition = new Vector2(
                    _titleArea.X + (_titleArea.Width - titleSize.X) / 2,
                    _titleArea.Y + (_titleArea.Height - titleSize.Y) / 2
                );
                spriteBatch.DrawString(_font, title, titlePosition, Color.White);
                
                // Draw menu options
                for (int i = 0; i < _menuOptions.Length; i++)
                {
                    Rectangle optionRect = _optionRectangles[i];
                    bool isSelected = i == _selectedIndex;
                    
                    // Draw option background
                    Color optionBg = isSelected ? new Color(80, 70, 100) : new Color(50, 45, 70);
                    spriteBatch.Draw(_pixelTexture, optionRect, optionBg);
                    
                    if (isSelected)
                    {
                        DrawBorder(spriteBatch, optionRect, Color.Yellow, 2);
                    }
                    
                    // Draw option text
                    string optionText = _menuOptions[i];
                    Vector2 textSize = _font.MeasureString(optionText);
                    Vector2 textPosition = new Vector2(
                        optionRect.X + (optionRect.Width - textSize.X) / 2,
                        optionRect.Y + (optionRect.Height - textSize.Y) / 2
                    );
                    
                    Color textColor = isSelected ? Color.Yellow : Color.White;
                    spriteBatch.DrawString(_font, optionText, textPosition, textColor);
                }
                
                // Draw instructions
                string instructions = "Use Up/Down arrows to navigate, Enter/Space to select, Escape to close";
                Vector2 instructionSize = _font.MeasureString(instructions);
                Vector2 instructionPosition = new Vector2(
                    _menuArea.X + (_menuArea.Width - instructionSize.X) / 2,
                    _menuArea.Bottom - 30
                );
                spriteBatch.DrawString(_font, instructions, instructionPosition, Color.LightGray);
            }
            else
            {
                // Fallback rendering without font
                DrawFallbackMenu(spriteBatch);
            }
        }

        /// <summary>
        /// Handles mouse click events for menu interaction
        /// </summary>
        /// <param name="mousePosition">Position of the mouse click</param>
        /// <returns>True if the click was handled by the menu</returns>
        public bool HandleMouseClick(Vector2 mousePosition)
        {
            if (!_isVisible) return false;
            
            // Check if click is within menu area
            if (!_menuArea.Contains(mousePosition)) return false;
            
            // Check which option was clicked
            for (int i = 0; i < _optionRectangles.Length; i++)
            {
                if (_optionRectangles[i].Contains(mousePosition))
                {
                    _selectedIndex = i;
                    SelectCurrentOption();
                    return true;
                }
            }
            
            return true; // Consume click even if no option was selected
        }

        /// <summary>
        /// Toggles the menu visibility and pause state
        /// </summary>
        public void ToggleMenu()
        {
            _isVisible = !_isVisible;
            _isPaused = _isVisible;
            
            // Call the pause function to update game state
            _setPauseFunction(_isPaused);
            
            // Reset selection when opening menu
            if (_isVisible)
            {
                _selectedIndex = 0;
            }
        }

        /// <summary>
        /// Shows the menu and pauses the game
        /// </summary>
        public void ShowMenu()
        {
            if (!_isVisible)
            {
                ToggleMenu();
            }
        }

        /// <summary>
        /// Hides the menu and resumes the game
        /// </summary>
        public void HideMenu()
        {
            if (_isVisible)
            {
                ToggleMenu();
            }
        }

        /// <summary>
        /// Selects the currently highlighted menu option
        /// </summary>
        private void SelectCurrentOption()
        {
            switch (_selectedIndex)
            {
                case 0: // Resume Game
                    HideMenu();
                    ResumeRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case 1: // Save Game
                    SaveRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case 2: // Load Game
                    LoadRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case 3: // Options
                    OptionsRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case 4: // Return to Main Menu
                    MainMenuRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        /// <summary>
        /// Draws a border around the specified rectangle
        /// </summary>
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

        /// <summary>
        /// Draws a fallback menu when no font is available
        /// </summary>
        private void DrawFallbackMenu(SpriteBatch spriteBatch)
        {
            // Draw simple colored rectangles for each option
            for (int i = 0; i < _menuOptions.Length; i++)
            {
                Rectangle optionRect = _optionRectangles[i];
                bool isSelected = i == _selectedIndex;
                
                Color optionColor = isSelected ? Color.Yellow : Color.Gray;
                spriteBatch.Draw(_pixelTexture, optionRect, optionColor);
                
                if (isSelected)
                {
                    DrawBorder(spriteBatch, optionRect, Color.White, 2);
                }
            }
        }

        /// <summary>
        /// Disposes of resources used by the escape menu UI
        /// </summary>
        public void Dispose()
        {
            _pixelTexture?.Dispose();
        }
    }
}