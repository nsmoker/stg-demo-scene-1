using Godot;
using System;
using System.Collections.Generic;

public static class InventorySystem
{
    private static Dictionary<string, List<Item>> _invMap = [];

    public delegate void InventoryChangeEvent(string entity, Item item, bool added);

    public static InventoryChangeEvent InventoryChangeHandlers;

    public static void SetInventory(string entity, List<Item> initialInventory)
    {
        _invMap.Add(entity, initialInventory);
    }

    public static void Remove(string entity)
    { 
        _invMap.Remove(entity); 
    }

    public static void Transfer(string fromEntity, string toEntity, Item itemToTransfer)
    {
        AddItem(toEntity, itemToTransfer);
        RemoveItem(fromEntity, itemToTransfer);
    }

    public static void AddItem(string entity, Item item)
    {
        _invMap[entity].Add(item);
        InventoryChangeHandlers?.Invoke(entity, item, true);
    }

    public static void RemoveItem(string entity, Item item)
    {
        _invMap[entity].Remove(item);
        InventoryChangeHandlers?.Invoke(entity, item, false);
    }

    public static List<Item> RetrieveInventory(string entity)
    {
        if (!_invMap.TryGetValue(entity, out List<Item> value))
        {
            return [];
        }
        else
        {
            return [ .. value];
        }
    }
}
