using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// UI for save slot selection, loading games, and creating new games
    /// </summary>
    public class SaveLoadUI
    {
        private SpriteFont _font;
        private Texture2D _pixelTexture;
        private SaveManager _saveManager;
        private SaveSlotManager _slotManager;
        
        private bool _isVisible;
        private bool _isInSaveMode; // true for save, false for load
        private Rectangle _uiArea;
        private Rectangle _titleArea;
        private Rectangle _slotListArea;
        private Rectangle _buttonArea;
        
        // Save slot data
        private List<SaveSlotInfo> _saveSlots;
        private int _highlightedSlotIndex;
        private int _scrollOffset;
        private const int MaxVisibleSlots = 6;
        private const int MaxSaveSlots = 2; // Temporarily limit to 2 slots to prevent crashes
        
        // UI state
        private bool _showConfirmDialog;
        private string _confirmMessage;
        private bool _confirmResult;
        private bool _isOperationInProgress;
        private string _progressMessage;
        private float _progressValue; // 0.0 to 1.0
        
        // Input handling
        private KeyboardState _previousKeyboardState;
        
        // Events
        public event EventHandler<SaveSlotSelectedEventArgs> SaveSlotSelected;
        public event EventHandler<LoadSlotSelectedEventArgs> LoadSlotSelected;
        public event EventHandler NewGameRequested;
        public event EventHandler BackRequested;

        public bool IsVisible => _isVisible;
        public bool IsOperationInProgress => _isOperationInProgress;

        /// <summary>
        /// Initializes a new instance of the SaveLoadUI
        /// </summary>
        /// <param name="graphicsDevice">Graphics device for creating textures</param>
        /// <param name="saveManager">Save manager for save/load operations</param>
        /// <param name="slotManager">Save slot manager for slot metadata</param>
        public SaveLoadUI(GraphicsDevice graphicsDevice, SaveManager saveManager, SaveSlotManager slotManager)
        {
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
            
            _saveSlots = new List<SaveSlotInfo>();
            _highlightedSlotIndex = 0;
            _scrollOffset = 0;
            
            // Create pixel texture for UI backgrounds
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
            // Define UI areas (centered on screen)
            int uiWidth = 700;
            int uiHeight = 500;
            int screenWidth = 1024; // Assuming default screen size
            int screenHeight = 768;
            
            _uiArea = new Rectangle(
                (screenWidth - uiWidth) / 2,
                (screenHeight - uiHeight) / 2,
                uiWidth,
                uiHeight
            );
            
            _titleArea = new Rectangle(_uiArea.X, _uiArea.Y, _uiArea.Width, 50);
            _slotListArea = new Rectangle(_uiArea.X + 10, _titleArea.Bottom + 10, _uiArea.Width - 20, 350);
            _buttonArea = new Rectangle(_uiArea.X + 10, _slotListArea.Bottom + 10, _uiArea.Width - 20, 40);
            
            _previousKeyboardState = Keyboard.GetState();
            
            // Subscribe to save manager events
            _saveManager.SaveCompleted += OnSaveCompleted;
            _saveManager.LoadCompleted += OnLoadCompleted;
            _saveManager.SaveError += OnSaveError;
            _saveManager.SaveProgressChanged += OnSaveProgressChanged;
        }

        /// <summary>
        /// Loads content for the save/load UI
        /// </summary>
        /// <param name="font">Font to use for text rendering</param>
        public void LoadContent(SpriteFont font)
        {
            _font = font;
        }

        /// <summary>
        /// Shows the save/load UI in the specified mode
        /// </summary>
        /// <param name="saveMode">True for save mode, false for load mode</param>
        public async void Show(bool saveMode)
        {
            _isVisible = true;
            _isInSaveMode = saveMode;
            _highlightedSlotIndex = 0; // Start with first slot selected
            _scrollOffset = 0;
            _showConfirmDialog = false;
            _isOperationInProgress = false;
            
            // Refresh save slot information
            await RefreshSaveSlots();
        }

        /// <summary>
        /// Hides the save/load UI
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            _showConfirmDialog = false;
            _isOperationInProgress = false;
        }

        /// <summary>
        /// Updates the save/load UI
        /// </summary>
        /// <param name="gameTime">Game time information</param>
        public void Update(GameTime gameTime)
        {
            if (!_isVisible) return;
            
            var currentKeyboardState = Keyboard.GetState();
            
            // Handle confirmation dialog input
            if (_showConfirmDialog)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Y) && !_previousKeyboardState.IsKeyDown(Keys.Y))
                {
                    _confirmResult = true;
                    _showConfirmDialog = false;
                    HandleConfirmResult();
                }
                else if (currentKeyboardState.IsKeyDown(Keys.N) && !_previousKeyboardState.IsKeyDown(Keys.N))
                {
                    _confirmResult = false;
                    _showConfirmDialog = false;
                }
            }
            // Handle normal UI input
            else if (!_isOperationInProgress)
            {
                // Navigate up
                if (currentKeyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
                {
                    if (_highlightedSlotIndex <= 0)
                        _highlightedSlotIndex = 0;
                    else
                        _highlightedSlotIndex = Math.Max(0, _highlightedSlotIndex - 1);
                    UpdateScrollOffset();
                }
                
                // Navigate down
                if (currentKeyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
                {
                    if (_highlightedSlotIndex < 0)
                        _highlightedSlotIndex = 0;
                    else
                        _highlightedSlotIndex = Math.Min(_saveSlots.Count - 1, _highlightedSlotIndex + 1);
                    UpdateScrollOffset();
                }
                
                // Select slot with Enter
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                {
                    System.Console.WriteLine($"⌨️ Enter key detected - performing action on selected slot {_highlightedSlotIndex + 1}");
                    ConfirmSlotAction();
                }
                
                // New game with N key (only in load mode)
                if (!_isInSaveMode && currentKeyboardState.IsKeyDown(Keys.N) && !_previousKeyboardState.IsKeyDown(Keys.N))
                {
                    NewGameRequested?.Invoke(this, EventArgs.Empty);
                }
                
                // Delete slot with Delete key
                if (currentKeyboardState.IsKeyDown(Keys.Delete) && !_previousKeyboardState.IsKeyDown(Keys.Delete))
                {
                    if (_highlightedSlotIndex < _saveSlots.Count && _saveSlots[_highlightedSlotIndex].HasSave)
                    {
                        ShowConfirmDialog($"Delete save slot {_saveSlots[_highlightedSlotIndex].SlotId}?");
                    }
                }
                
                // Back with Escape
                if (currentKeyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
                {
                    BackRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            
            _previousKeyboardState = currentKeyboardState;
        }

        /// <summary>
        /// Draws the save/load UI
        /// </summary>
        /// <param name="spriteBatch">Sprite batch for rendering</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible) return;
            
            // Draw semi-transparent background overlay
            Rectangle screenRect = new Rectangle(0, 0, 1024, 768);
            spriteBatch.Draw(_pixelTexture, screenRect, Color.Black * 0.8f);
            
            // Draw main UI background
            spriteBatch.Draw(_pixelTexture, _uiArea, new Color(40, 35, 60));
            DrawBorder(spriteBatch, _uiArea, Color.White, 3);
            
            // Draw title area
            spriteBatch.Draw(_pixelTexture, _titleArea, new Color(60, 50, 80));
            
            if (_font != null)
            {
                DrawWithFont(spriteBatch);
            }
            else
            {
                DrawFallback(spriteBatch);
            }
            
            // Draw confirmation dialog if visible
            if (_showConfirmDialog)
            {
                DrawConfirmDialog(spriteBatch);
            }
            
            // Draw progress indicator if operation is in progress
            if (_isOperationInProgress)
            {
                DrawProgressIndicator(spriteBatch);
            }
        }

        /// <summary>
        /// Handles mouse click events
        /// </summary>
        /// <param name="mousePosition">Position of the mouse click</param>
        /// <returns>True if the click was handled</returns>
        public bool HandleMouseClick(Vector2 mousePosition)
        {
            if (!_isVisible || _isOperationInProgress) return false;
            
            // Check if click is within UI area
            if (!_uiArea.Contains(mousePosition)) return false;
            
            // Handle confirmation dialog clicks
            if (_showConfirmDialog)
            {
                // Simple Y/N handling - could be enhanced with buttons
                return true;
            }
            
            // Check slot list clicks
            if (_slotListArea.Contains(mousePosition))
            {
                int slotHeight = 55;
                int clickedIndex = (int)(mousePosition.Y - _slotListArea.Y) / slotHeight + _scrollOffset;
                
                if (clickedIndex >= 0 && clickedIndex < _saveSlots.Count)
                {
                    System.Console.WriteLine($"🖱️ Mouse clicked on slot {clickedIndex + 1} - just highlighting, not performing action");
                    _highlightedSlotIndex = clickedIndex;
                    System.Console.WriteLine($"🎯 Slot {_highlightedSlotIndex + 1} is now highlighted. Press Enter to confirm save/load.");
                }
                return true;
            }
            
            return true; // Consume click
        }

        /// <summary>
        /// Refreshes the save slot information from the save manager
        /// </summary>
        private async Task RefreshSaveSlots()
        {
            _saveSlots.Clear();
            
            try
            {
                // Get all available save slots
                var slotInfos = await _slotManager.GetAllSlotInfoAsync();
                
                // Create entries for all possible slots (1 to MaxSaveSlots)
                for (int i = 1; i <= MaxSaveSlots; i++)
                {
                    if (slotInfos.ContainsKey(i))
                    {
                        System.Console.WriteLine($"RefreshSaveSlots: Found save in slot {i} - {slotInfos[i].CurrentZoneName}");
                        _saveSlots.Add(new SaveSlotInfo
                        {
                            SlotId = i,
                            HasSave = true,
                            Metadata = slotInfos[i]
                        });
                    }
                    else
                    {
                        System.Console.WriteLine($"RefreshSaveSlots: No save found in slot {i}");
                        _saveSlots.Add(new SaveSlotInfo
                        {
                            SlotId = i,
                            HasSave = false,
                            Metadata = null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error refreshing save slots: {ex.Message}");
                
                // Create empty slots as fallback
                for (int i = 1; i <= MaxSaveSlots; i++)
                {
                    _saveSlots.Add(new SaveSlotInfo
                    {
                        SlotId = i,
                        HasSave = false,
                        Metadata = null
                    });
                }
            }
        }

        /// <summary>
        /// Draws the UI with font support
        /// </summary>
        private void DrawWithFont(SpriteBatch spriteBatch)
        {
            // Draw title
            string title = _isInSaveMode ? "Save Game" : "Load Game";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePosition = new Vector2(
                _titleArea.X + (_titleArea.Width - titleSize.X) / 2,
                _titleArea.Y + (_titleArea.Height - titleSize.Y) / 2
            );
            spriteBatch.DrawString(_font, title, titlePosition, Color.White);
            
            // Draw save slots
            DrawSaveSlots(spriteBatch);
            
            // Draw instructions
            string instructions = _isInSaveMode ? 
                "Select slot to save | Delete to remove | Escape to cancel" :
                "Select slot to load | N for new game | Delete to remove | Escape to cancel";
            
            Vector2 instructionSize = _font.MeasureString(instructions);
            Vector2 instructionPosition = new Vector2(
                _buttonArea.X + (_buttonArea.Width - instructionSize.X) / 2,
                _buttonArea.Y + (_buttonArea.Height - instructionSize.Y) / 2
            );
            spriteBatch.DrawString(_font, instructions, instructionPosition, Color.LightGray);
        }

        /// <summary>
        /// Draws the save slots list
        /// </summary>
        private void DrawSaveSlots(SpriteBatch spriteBatch)
        {
            // Draw slot list background
            spriteBatch.Draw(_pixelTexture, _slotListArea, new Color(30, 25, 50));
            
            int slotHeight = 55;
            int visibleSlots = Math.Min(MaxVisibleSlots, _saveSlots.Count - _scrollOffset);
            
            for (int i = 0; i < visibleSlots; i++)
            {
                int slotIndex = i + _scrollOffset;
                var slotInfo = _saveSlots[slotIndex];
                bool isSelected = slotIndex == _highlightedSlotIndex;
                
                Rectangle slotRect = new Rectangle(
                    _slotListArea.X + 5,
                    _slotListArea.Y + 5 + i * slotHeight,
                    _slotListArea.Width - 10,
                    slotHeight - 5
                );
                
                // Draw slot background
                Color slotBg = isSelected ? new Color(80, 70, 100) : new Color(50, 45, 70);
                spriteBatch.Draw(_pixelTexture, slotRect, slotBg);
                
                if (isSelected)
                {
                    DrawBorder(spriteBatch, slotRect, Color.Yellow, 2);
                }
                
                // Draw slot content
                Vector2 slotPosition = new Vector2(slotRect.X + 10, slotRect.Y + 5);
                
                if (slotInfo.HasSave && slotInfo.Metadata != null)
                {
                    // Draw save information
                    string slotTitle = $"Slot {slotInfo.SlotId}: {slotInfo.Metadata.CurrentZoneName}";
                    string slotDetails = $"Day {slotInfo.Metadata.CurrentDay} | {slotInfo.Metadata.PlayTime:hh\\:mm\\:ss} | {slotInfo.Metadata.LastSaveTime:MM/dd/yyyy HH:mm}";
                    string slotProgress = $"{slotInfo.Metadata.ZonesVisited} zones | {slotInfo.Metadata.JournalEntries} entries";
                    
                    spriteBatch.DrawString(_font, slotTitle, slotPosition, Color.White);
                    spriteBatch.DrawString(_font, slotDetails, slotPosition + new Vector2(0, 18), Color.LightGray);
                    spriteBatch.DrawString(_font, slotProgress, slotPosition + new Vector2(0, 32), Color.LightBlue);
                }
                else
                {
                    // Draw empty slot
                    string emptyText = $"Slot {slotInfo.SlotId}: Empty";
                    spriteBatch.DrawString(_font, emptyText, slotPosition + new Vector2(0, 15), Color.Gray);
                }
            }
            
            // Draw scroll indicator if needed
            if (_saveSlots.Count > MaxVisibleSlots)
            {
                DrawScrollIndicator(spriteBatch);
            }
        }

        /// <summary>
        /// Draws a scroll indicator for the save slots list
        /// </summary>
        private void DrawScrollIndicator(SpriteBatch spriteBatch)
        {
            float scrollPercent = (float)_scrollOffset / (_saveSlots.Count - MaxVisibleSlots);
            int scrollBarHeight = _slotListArea.Height - 20;
            int scrollThumbHeight = 30;
            int scrollThumbY = (int)(scrollPercent * (scrollBarHeight - scrollThumbHeight));
            
            Rectangle scrollBar = new Rectangle(_slotListArea.Right - 15, _slotListArea.Y + 10, 8, scrollBarHeight);
            Rectangle scrollThumb = new Rectangle(_slotListArea.Right - 15, _slotListArea.Y + 10 + scrollThumbY, 8, scrollThumbHeight);
            
            spriteBatch.Draw(_pixelTexture, scrollBar, Color.DarkGray);
            spriteBatch.Draw(_pixelTexture, scrollThumb, Color.LightGray);
        }

        /// <summary>
        /// Draws a fallback UI when no font is available
        /// </summary>
        private void DrawFallback(SpriteBatch spriteBatch)
        {
            // Draw simple colored rectangles for slots
            int slotHeight = 55;
            int visibleSlots = Math.Min(MaxVisibleSlots, _saveSlots.Count - _scrollOffset);
            
            for (int i = 0; i < visibleSlots; i++)
            {
                int slotIndex = i + _scrollOffset;
                var slotInfo = _saveSlots[slotIndex];
                bool isSelected = slotIndex == _highlightedSlotIndex;
                
                Rectangle slotRect = new Rectangle(
                    _slotListArea.X + 5,
                    _slotListArea.Y + 5 + i * slotHeight,
                    _slotListArea.Width - 10,
                    slotHeight - 5
                );
                
                Color slotColor = slotInfo.HasSave ? 
                    (isSelected ? Color.Yellow : Color.Green) :
                    (isSelected ? Color.Orange : Color.Gray);
                
                spriteBatch.Draw(_pixelTexture, slotRect, slotColor);
                
                if (isSelected)
                {
                    DrawBorder(spriteBatch, slotRect, Color.White, 2);
                }
            }
        }

        /// <summary>
        /// Draws a confirmation dialog
        /// </summary>
        private void DrawConfirmDialog(SpriteBatch spriteBatch)
        {
            Rectangle dialogArea = new Rectangle(
                _uiArea.X + 100,
                _uiArea.Y + 150,
                _uiArea.Width - 200,
                100
            );
            
            // Draw dialog background
            spriteBatch.Draw(_pixelTexture, dialogArea, new Color(60, 50, 80));
            DrawBorder(spriteBatch, dialogArea, Color.Red, 2);
            
            if (_font != null)
            {
                // Draw confirmation message
                Vector2 messageSize = _font.MeasureString(_confirmMessage);
                Vector2 messagePosition = new Vector2(
                    dialogArea.X + (dialogArea.Width - messageSize.X) / 2,
                    dialogArea.Y + 20
                );
                spriteBatch.DrawString(_font, _confirmMessage, messagePosition, Color.White);
                
                // Draw Y/N prompt
                string prompt = "Press Y to confirm, N to cancel";
                Vector2 promptSize = _font.MeasureString(prompt);
                Vector2 promptPosition = new Vector2(
                    dialogArea.X + (dialogArea.Width - promptSize.X) / 2,
                    dialogArea.Y + 50
                );
                spriteBatch.DrawString(_font, prompt, promptPosition, Color.Yellow);
            }
        }

        /// <summary>
        /// Draws a progress indicator for save/load operations
        /// </summary>
        private void DrawProgressIndicator(SpriteBatch spriteBatch)
        {
            Rectangle progressArea = new Rectangle(
                _uiArea.X + 50,
                _uiArea.Y + 200,
                _uiArea.Width - 100,
                80
            );
            
            // Draw progress background
            spriteBatch.Draw(_pixelTexture, progressArea, new Color(40, 35, 60));
            DrawBorder(spriteBatch, progressArea, Color.Blue, 2);
            
            if (_font != null)
            {
                // Draw progress message
                Vector2 messageSize = _font.MeasureString(_progressMessage);
                Vector2 messagePosition = new Vector2(
                    progressArea.X + (progressArea.Width - messageSize.X) / 2,
                    progressArea.Y + 15
                );
                spriteBatch.DrawString(_font, _progressMessage, messagePosition, Color.White);
                
                // Draw progress bar
                Rectangle progressBar = new Rectangle(
                    progressArea.X + 20,
                    progressArea.Y + 45,
                    progressArea.Width - 40,
                    15
                );
                
                spriteBatch.Draw(_pixelTexture, progressBar, Color.DarkGray);
                
                int fillWidth = (int)(progressBar.Width * _progressValue);
                Rectangle progressFill = new Rectangle(
                    progressBar.X,
                    progressBar.Y,
                    fillWidth,
                    progressBar.Height
                );
                
                spriteBatch.Draw(_pixelTexture, progressFill, Color.Blue);
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
        /// Updates the scroll offset based on the selected slot
        /// </summary>
        private void UpdateScrollOffset()
        {
            if (_highlightedSlotIndex < _scrollOffset)
            {
                _scrollOffset = _highlightedSlotIndex;
            }
            else if (_highlightedSlotIndex >= _scrollOffset + MaxVisibleSlots)
            {
                _scrollOffset = _highlightedSlotIndex - MaxVisibleSlots + 1;
            }
        }

        /// <summary>
        /// Selects the currently highlighted slot
        /// </summary>
        private void ConfirmSlotAction()
        {
            System.Console.WriteLine($"🔍 SelectCurrentSlot: _highlightedSlotIndex={_highlightedSlotIndex}, _saveSlots.Count={_saveSlots.Count}");
            
            if (_highlightedSlotIndex < 0) 
            {
                System.Console.WriteLine($"❌ No slot selected yet - use arrow keys to select a slot first");
                return;
            }
            
            if (_highlightedSlotIndex >= _saveSlots.Count) 
            {
                System.Console.WriteLine($"❌ Invalid slot index: {_highlightedSlotIndex} (max: {_saveSlots.Count - 1})");
                return;
            }
            
            var selectedSlot = _saveSlots[_highlightedSlotIndex];
            
            System.Console.WriteLine($"🎯 SelectCurrentSlot: SlotId={selectedSlot.SlotId}, HasSave={selectedSlot.HasSave}, IsInSaveMode={_isInSaveMode}");
            
            if (_isInSaveMode)
            {
                // Save mode - check if slot has existing save
                if (selectedSlot.HasSave)
                {
                    ShowConfirmDialog($"Overwrite save in slot {selectedSlot.SlotId}?");
                }
                else
                {
                    PerformSave(selectedSlot.SlotId);
                }
            }
            else
            {
                // Load mode - only allow loading if slot has save
                if (selectedSlot.HasSave)
                {
                    System.Console.WriteLine($"📊 Slot {selectedSlot.SlotId} has save data, calling PerformLoad...");
                    PerformLoad(selectedSlot.SlotId);
                }
                else
                {
                    System.Console.WriteLine($"❌ Cannot load slot {selectedSlot.SlotId} - no save data");
                }
            }
        }

        /// <summary>
        /// Shows a confirmation dialog with the specified message
        /// </summary>
        private void ShowConfirmDialog(string message)
        {
            _confirmMessage = message;
            _showConfirmDialog = true;
        }

        /// <summary>
        /// Handles the result of a confirmation dialog
        /// </summary>
        private void HandleConfirmResult()
        {
            if (!_confirmResult) return;
            
            var selectedSlot = _saveSlots[_highlightedSlotIndex];
            
            if (_confirmMessage.Contains("Overwrite"))
            {
                PerformSave(selectedSlot.SlotId);
            }
            else if (_confirmMessage.Contains("Delete"))
            {
                PerformDelete(selectedSlot.SlotId);
            }
        }

        /// <summary>
        /// Performs a save operation to the specified slot
        /// </summary>
        private void PerformSave(int slotId)
        {
            _isOperationInProgress = true;
            _progressMessage = $"Saving to slot {slotId}...";
            _progressValue = 0.0f;
            
            try
            {
                SaveSlotSelected?.Invoke(this, new SaveSlotSelectedEventArgs { SlotId = slotId });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error during save: {ex.Message}");
                _isOperationInProgress = false;
            }
        }

        /// <summary>
        /// Performs a load operation from the specified slot
        /// </summary>
        private void PerformLoad(int slotId)
        {
            System.Console.WriteLine($"🎯 PerformLoad called for slot {slotId}");
            _isOperationInProgress = true;
            _progressMessage = $"Loading from slot {slotId}...";
            _progressValue = 0.0f;
            
            try
            {
                System.Console.WriteLine($"📊 About to fire LoadSlotSelected event for slot {slotId}");
                LoadSlotSelected?.Invoke(this, new LoadSlotSelectedEventArgs { SlotId = slotId });
                System.Console.WriteLine($"✅ LoadSlotSelected event fired successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in PerformLoad: {ex.GetType().Name}: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _isOperationInProgress = false;
            }
        }

        /// <summary>
        /// Performs a delete operation on the specified slot
        /// </summary>
        private async void PerformDelete(int slotId)
        {
            try
            {
                await _slotManager.DeleteSlotAsync(slotId);
                await RefreshSaveSlots();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error deleting slot {slotId}: {ex.Message}");
            }
        }

        // Event handlers for save manager events
        private async void OnSaveCompleted(object sender, SaveCompletedEventArgs e)
        {
            _isOperationInProgress = false;
            await RefreshSaveSlots();
        }

        private void OnLoadCompleted(object sender, LoadCompletedEventArgs e)
        {
            _isOperationInProgress = false;
            Hide(); // Close UI after successful load
        }

        private void OnSaveError(object sender, SaveErrorEventArgs e)
        {
            _isOperationInProgress = false;
            _progressMessage = $"Error: {e.ErrorMessage}";
        }

        private void OnSaveProgressChanged(object sender, SaveProgressEventArgs e)
        {
            _progressValue = e.Progress.ProgressPercentage;
            _progressMessage = e.Progress.CurrentStep ?? _progressMessage;
        }

        /// <summary>
        /// Disposes of resources used by the save/load UI
        /// </summary>
        public void Dispose()
        {
            _pixelTexture?.Dispose();
            
            // Unsubscribe from events
            if (_saveManager != null)
            {
                _saveManager.SaveCompleted -= OnSaveCompleted;
                _saveManager.LoadCompleted -= OnLoadCompleted;
                _saveManager.SaveError -= OnSaveError;
                _saveManager.SaveProgressChanged -= OnSaveProgressChanged;
            }
        }
    }

    /// <summary>
    /// Information about a save slot for UI display
    /// </summary>
    public class SaveSlotInfo
    {
        public int SlotId { get; set; }
        public bool HasSave { get; set; }
        public SaveSlotMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Event arguments for save slot selection
    /// </summary>
    public class SaveSlotSelectedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
    }

    /// <summary>
    /// Event arguments for load slot selection
    /// </summary>
    public class LoadSlotSelectedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
    }
}