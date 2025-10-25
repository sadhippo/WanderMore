using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class InventoryUI
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private InventoryManager _inventoryManager;
    private bool _isVisible;
    private Rectangle _windowArea;
    private Rectangle _closeButtonArea;
    private List<Rectangle> _itemSlots;
    private int _scrollOffset;
    private const int ITEMS_PER_ROW = 6;
    private const int VISIBLE_ROWS = 5;
    private const int SLOT_SIZE = 80;
    private const int SLOT_PADDING = 5;
    private Vector2 _lastMousePosition;
    private string _hoveredItemId;
    private Rectangle _tooltipArea;

    public bool IsVisible => _isVisible;

    public InventoryUI(GraphicsDevice graphicsDevice)
    {
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        _isVisible = false;
        _scrollOffset = 0;
        _itemSlots = new List<Rectangle>();
        
        // Initialize window area (centered on screen)
        int windowWidth = (ITEMS_PER_ROW * SLOT_SIZE) + ((ITEMS_PER_ROW + 1) * SLOT_PADDING) + 20;
        int windowHeight = (VISIBLE_ROWS * SLOT_SIZE) + ((VISIBLE_ROWS + 1) * SLOT_PADDING) + 80; // Extra space for header
        
        _windowArea = new Rectangle(
            (1024 - windowWidth) / 2,
            (768 - windowHeight) / 2,
            windowWidth,
            windowHeight
        );
        
        _closeButtonArea = new Rectangle(
            _windowArea.Right - 30,
            _windowArea.Y + 5,
            25,
            25
        );
        
        SetupItemSlots();
    }

    private void SetupItemSlots()
    {
        _itemSlots.Clear();
        
        int startX = _windowArea.X + 10 + SLOT_PADDING;
        int startY = _windowArea.Y + 50; // Leave space for header
        
        for (int row = 0; row < VISIBLE_ROWS; row++)
        {
            for (int col = 0; col < ITEMS_PER_ROW; col++)
            {
                int x = startX + col * (SLOT_SIZE + SLOT_PADDING);
                int y = startY + row * (SLOT_SIZE + SLOT_PADDING);
                
                _itemSlots.Add(new Rectangle(x, y, SLOT_SIZE, SLOT_SIZE));
            }
        }
    }

    public void SetInventoryManager(InventoryManager inventoryManager)
    {
        _inventoryManager = inventoryManager;
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        // Recenter the window
        _windowArea.X = (screenWidth - _windowArea.Width) / 2;
        _windowArea.Y = (screenHeight - _windowArea.Height) / 2;
        
        _closeButtonArea.X = _windowArea.Right - 30;
        _closeButtonArea.Y = _windowArea.Y + 5;
        
        SetupItemSlots();
    }

    public void Show()
    {
        _isVisible = true;
        _scrollOffset = 0;
    }

    public void Hide()
    {
        _isVisible = false;
    }

    public void Toggle()
    {
        if (_isVisible)
            Hide();
        else
            Show();
    }

    public void Update(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        // Handle scrolling
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Up) && _scrollOffset > 0)
        {
            _scrollOffset--;
        }
        else if (keyboardState.IsKeyDown(Keys.Down))
        {
            int maxScroll = Math.Max(0, GetTotalRows() - VISIBLE_ROWS);
            if (_scrollOffset < maxScroll)
            {
                _scrollOffset++;
            }
        }
    }

    private int GetTotalRows()
    {
        if (_inventoryManager == null) return 0;
        
        int itemCount = _inventoryManager.GetUsedSlots();
        return (int)Math.Ceiling((double)itemCount / ITEMS_PER_ROW);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isVisible || _inventoryManager == null) return;

        // Draw window background
        DrawPanel(spriteBatch, _windowArea, new Color(40, 40, 40, 240));
        
        // Draw window border
        DrawBorder(spriteBatch, _windowArea, Color.White, 2);
        
        // Draw header
        if (_font != null)
        {
            string headerText = $"Inventory ({_inventoryManager.GetUsedSlots()}/{_inventoryManager.MaxSlots})";
            Vector2 headerPos = new Vector2(_windowArea.X + 10, _windowArea.Y + 10);
            spriteBatch.DrawString(_font, headerText, headerPos, Color.White);
        }
        
        // Draw close button
        DrawPanel(spriteBatch, _closeButtonArea, Color.Red);
        if (_font != null)
        {
            Vector2 closeTextPos = new Vector2(_closeButtonArea.X + 8, _closeButtonArea.Y + 5);
            spriteBatch.DrawString(_font, "X", closeTextPos, Color.White);
        }
        
        // Draw item slots
        DrawItemSlots(spriteBatch);
        
        // Draw tooltip if hovering over an item
        DrawTooltip(spriteBatch);
    }

    private void DrawItemSlots(SpriteBatch spriteBatch)
    {
        var items = _inventoryManager.GetAllItems().ToList();
        int startIndex = _scrollOffset * ITEMS_PER_ROW;
        
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            int itemIndex = startIndex + i;
            
            // Draw slot background
            Color slotColor = new Color(60, 60, 60, 200);
            DrawPanel(spriteBatch, slot, slotColor);
            DrawBorder(spriteBatch, slot, Color.Gray, 1);
            
            // Draw item if exists
            if (itemIndex < items.Count)
            {
                var item = items[itemIndex];
                DrawItem(spriteBatch, slot, item);
            }
        }
    }

    private void DrawItem(SpriteBatch spriteBatch, Rectangle slot, InventoryItem item)
    {
        // Draw item background based on rarity
        Color itemBgColor = GetRarityColor(item.Rarity);
        Rectangle itemBg = new Rectangle(slot.X + 2, slot.Y + 2, slot.Width - 4, slot.Height - 4);
        DrawPanel(spriteBatch, itemBg, itemBgColor * 0.3f);
        
        // Draw item icon placeholder (colored rectangle for now)
        Color itemColor = GetItemTypeColor(item.Type);
        Rectangle iconArea = new Rectangle(slot.X + 10, slot.Y + 10, slot.Width - 20, slot.Height - 35);
        DrawPanel(spriteBatch, iconArea, itemColor);
        
        if (_font != null)
        {
            // Draw item name (truncated)
            string displayName = item.Name.Length > 8 ? item.Name.Substring(0, 8) + "..." : item.Name;
            Vector2 namePos = new Vector2(slot.X + 5, slot.Y + slot.Height - 25);
            spriteBatch.DrawString(_font, displayName, namePos, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            
            // Draw quantity
            if (item.Quantity > 1)
            {
                string quantityText = item.Quantity.ToString();
                Vector2 quantitySize = _font.MeasureString(quantityText) * 0.7f;
                Vector2 quantityPos = new Vector2(
                    slot.Right - quantitySize.X - 5,
                    slot.Y + 5
                );
                
                // Draw quantity background
                Rectangle quantityBg = new Rectangle(
                    (int)quantityPos.X - 2,
                    (int)quantityPos.Y - 1,
                    (int)quantitySize.X + 4,
                    (int)quantitySize.Y + 2
                );
                DrawPanel(spriteBatch, quantityBg, Color.Black * 0.7f);
                
                spriteBatch.DrawString(_font, quantityText, quantityPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawTooltip(SpriteBatch spriteBatch)
    {
        if (string.IsNullOrEmpty(_hoveredItemId) || _font == null) return;
        
        var item = _inventoryManager.GetItem(_hoveredItemId);
        if (item == null) return;
        
        // Create tooltip text
        string tooltipText = $"{item.Name}\n{item.Description}\nQuantity: {item.Quantity}\nType: {item.Type}\nRarity: {item.Rarity}";
        
        Vector2 textSize = _font.MeasureString(tooltipText);
        Vector2 tooltipPos = _lastMousePosition + new Vector2(15, 15);
        
        // Keep tooltip on screen
        if (tooltipPos.X + textSize.X > _windowArea.Right)
            tooltipPos.X = _lastMousePosition.X - textSize.X - 15;
        if (tooltipPos.Y + textSize.Y > _windowArea.Bottom)
            tooltipPos.Y = _lastMousePosition.Y - textSize.Y - 15;
        
        Rectangle tooltipBg = new Rectangle(
            (int)tooltipPos.X - 5,
            (int)tooltipPos.Y - 5,
            (int)textSize.X + 10,
            (int)textSize.Y + 10
        );
        
        // Draw tooltip background
        DrawPanel(spriteBatch, tooltipBg, new Color(20, 20, 20, 240));
        DrawBorder(spriteBatch, tooltipBg, Color.White, 1);
        
        // Draw tooltip text
        spriteBatch.DrawString(_font, tooltipText, tooltipPos, Color.White);
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => Color.Gray,
            ItemRarity.Uncommon => Color.Green,
            ItemRarity.Rare => Color.Blue,
            ItemRarity.Epic => Color.Purple,
            _ => Color.White
        };
    }

    private Color GetItemTypeColor(ItemType type)
    {
        return type switch
        {
            ItemType.Food => Color.Orange,
            ItemType.Collectible => Color.Yellow,
            ItemType.Material => Color.Brown,
            ItemType.Tool => Color.Silver,
            _ => Color.White
        };
    }

    private void DrawPanel(SpriteBatch spriteBatch, Rectangle area, Color color)
    {
        spriteBatch.Draw(_pixelTexture, area, color);
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
        
        // Check close button
        if (_closeButtonArea.Contains(mousePosition))
        {
            Hide();
            return true;
        }
        
        // Check item slots for future interactions (right-click to use, etc.)
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            if (_itemSlots[i].Contains(mousePosition))
            {
                var items = _inventoryManager.GetAllItems().ToList();
                int itemIndex = (_scrollOffset * ITEMS_PER_ROW) + i;
                
                if (itemIndex < items.Count)
                {
                    var item = items[itemIndex];
                    System.Console.WriteLine($"[INVENTORY] Clicked on {item.Name} (x{item.Quantity})");
                    // Future: Add item interaction logic here
                }
                return true;
            }
        }
        
        // Click was inside window area
        return _windowArea.Contains(mousePosition);
    }

    public bool HandleMouseHover(Vector2 mousePosition)
    {
        if (!_isVisible) return false;
        
        _lastMousePosition = mousePosition;
        _hoveredItemId = null;
        
        // Check if hovering over an item slot
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            if (_itemSlots[i].Contains(mousePosition))
            {
                var items = _inventoryManager.GetAllItems().ToList();
                int itemIndex = (_scrollOffset * ITEMS_PER_ROW) + i;
                
                if (itemIndex < items.Count)
                {
                    _hoveredItemId = items[itemIndex].Id;
                }
                return true;
            }
        }
        
        return _windowArea.Contains(mousePosition);
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}