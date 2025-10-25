using System;

namespace HiddenHorizons;

public class InventoryItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ItemType Type { get; set; }
    public ItemRarity Rarity { get; set; }
    public int Quantity { get; set; }
    public int MaxStackSize { get; set; }
    public bool IsStackable => MaxStackSize > 1;

    public InventoryItem(string id, string name, string description, ItemType type, ItemRarity rarity = ItemRarity.Common, int maxStackSize = 99)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
        Rarity = rarity;
        Quantity = 0;
        MaxStackSize = maxStackSize;
    }

    public InventoryItem Clone()
    {
        return new InventoryItem(Id, Name, Description, Type, Rarity, MaxStackSize)
        {
            Quantity = Quantity
        };
    }

    public bool CanStackWith(InventoryItem other)
    {
        return other != null && 
               Id == other.Id && 
               IsStackable && 
               other.IsStackable;
    }
    
    public bool HasSpaceForMore()
    {
        return IsStackable && Quantity < MaxStackSize;
    }

    public int AddQuantity(int amount)
    {
        int canAdd = Math.Min(amount, MaxStackSize - Quantity);
        Quantity += canAdd;
        return amount - canAdd; // Return overflow
    }

    public int RemoveQuantity(int amount)
    {
        int toRemove = Math.Min(amount, Quantity);
        Quantity -= toRemove;
        return toRemove;
    }
}