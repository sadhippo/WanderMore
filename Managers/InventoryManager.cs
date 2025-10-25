using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class InventoryManager
{
    private List<InventoryItem> _items;
    private Dictionary<string, InventoryItem> _itemTemplates;
    public int MaxSlots { get; private set; }
    
    public event Action<InventoryItem, int> ItemAdded;
    public event Action<InventoryItem, int> ItemRemoved;
    public event Action InventoryChanged;

    public InventoryManager(int maxSlots = 30)
    {
        MaxSlots = maxSlots;
        _items = new List<InventoryItem>();
        _itemTemplates = new Dictionary<string, InventoryItem>();
        
        InitializeItemTemplates();
    }

    private void InitializeItemTemplates()
    {
        // Food items
        _itemTemplates["berries"] = new InventoryItem("berries", "Wild Berries", "Sweet berries found on bushes", ItemType.Food, ItemRarity.Common, 20);
        _itemTemplates["mushroom"] = new InventoryItem("mushroom", "Mushroom", "A common forest mushroom", ItemType.Food, ItemRarity.Common, 10);
        _itemTemplates["fish"] = new InventoryItem("fish", "Fresh Fish", "A fish caught from the lake", ItemType.Food, ItemRarity.Uncommon, 5);
        _itemTemplates["egg"] = new InventoryItem("egg", "Fresh Egg", "A nutritious egg from a friendly chicken", ItemType.Food, ItemRarity.Common, 12);
        
        // Collectibles
        _itemTemplates["shiny_stone"] = new InventoryItem("shiny_stone", "Shiny Stone", "A mysterious glowing stone", ItemType.Collectible, ItemRarity.Rare, 1);
        _itemTemplates["flower"] = new InventoryItem("flower", "Wildflower", "A beautiful wildflower", ItemType.Collectible, ItemRarity.Common, 50);
        _itemTemplates["feather"] = new InventoryItem("feather", "Bird Feather", "A colorful feather from a forest bird", ItemType.Collectible, ItemRarity.Common, 25);
        
        // Materials
        _itemTemplates["wood"] = new InventoryItem("wood", "Wood", "Sturdy wood from trees", ItemType.Material, ItemRarity.Common, 50);
        _itemTemplates["stone"] = new InventoryItem("stone", "Stone", "A piece of solid rock", ItemType.Material, ItemRarity.Common, 99);
        _itemTemplates["herb"] = new InventoryItem("herb", "Medicinal Herb", "A healing herb", ItemType.Material, ItemRarity.Uncommon, 15);
    }

    public List<InventoryItem> GetAllItems()
    {
        return _items.Where(item => item.Quantity > 0).ToList();
    }

    public InventoryItem GetItem(string itemId)
    {
        return _items.FirstOrDefault(item => item.Id == itemId && item.Quantity > 0);
    }

    public int GetItemCount(string itemId)
    {
        return _items.Where(item => item.Id == itemId).Sum(item => item.Quantity);
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        return GetItemCount(itemId) >= quantity;
    }

    public bool AddItem(string itemId, int quantity = 1)
    {
        if (!_itemTemplates.ContainsKey(itemId))
        {
            System.Console.WriteLine($"[INVENTORY] Unknown item: {itemId}");
            return false;
        }

        var template = _itemTemplates[itemId];
        int remainingQuantity = quantity;

        // Try to stack with existing items first
        var existingItems = _items.Where(item => item.Id == itemId && item.HasSpaceForMore()).ToList();
        
        foreach (var existingItem in existingItems)
        {
            if (remainingQuantity <= 0) break;
            
            int overflow = existingItem.AddQuantity(remainingQuantity);
            int added = remainingQuantity - overflow;
            remainingQuantity = overflow;
            
            if (added > 0)
            {
                ItemAdded?.Invoke(existingItem, added);
            }
        }

        // Create new stacks if needed
        while (remainingQuantity > 0 && _items.Count < MaxSlots)
        {
            var newItem = template.Clone();
            int toAdd = Math.Min(remainingQuantity, newItem.MaxStackSize);
            newItem.Quantity = toAdd;
            
            _items.Add(newItem);
            remainingQuantity -= toAdd;
            
            ItemAdded?.Invoke(newItem, toAdd);
        }

        if (remainingQuantity > 0)
        {
            System.Console.WriteLine($"[INVENTORY] Could not add {remainingQuantity} {itemId} - inventory full");
        }

        InventoryChanged?.Invoke();
        return remainingQuantity == 0;
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (!HasItem(itemId, quantity))
        {
            return false;
        }

        int remainingToRemove = quantity;
        var itemsToRemove = new List<InventoryItem>();

        // Remove from existing stacks
        foreach (var item in _items.Where(item => item.Id == itemId && item.Quantity > 0).ToList())
        {
            if (remainingToRemove <= 0) break;

            int removed = item.RemoveQuantity(remainingToRemove);
            remainingToRemove -= removed;

            ItemRemoved?.Invoke(item, removed);

            if (item.Quantity <= 0)
            {
                itemsToRemove.Add(item);
            }
        }

        // Clean up empty stacks
        foreach (var item in itemsToRemove)
        {
            _items.Remove(item);
        }

        InventoryChanged?.Invoke();
        return remainingToRemove == 0;
    }

    public bool UseItem(string itemId, int quantity = 1)
    {
        if (!HasItem(itemId, quantity))
        {
            return false;
        }

        // For now, just remove the item
        // Later we can add specific use effects
        return RemoveItem(itemId, quantity);
    }

    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        return _items.Where(item => item.Type == type && item.Quantity > 0).ToList();
    }

    public int GetUsedSlots()
    {
        return _items.Count(item => item.Quantity > 0);
    }

    public int GetFreeSlots()
    {
        return MaxSlots - GetUsedSlots();
    }

    public bool IsFull()
    {
        return GetFreeSlots() <= 0;
    }

    public void Clear()
    {
        _items.Clear();
        InventoryChanged?.Invoke();
    }

    // Helper method to get item template for UI display
    public InventoryItem GetItemTemplate(string itemId)
    {
        return _itemTemplates.ContainsKey(itemId) ? _itemTemplates[itemId] : null;
    }

    public Dictionary<string, InventoryItem> GetAllItemTemplates()
    {
        return new Dictionary<string, InventoryItem>(_itemTemplates);
    }
}