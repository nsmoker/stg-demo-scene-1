using Godot;
using System;
using System.Collections.Generic;

public static class InventorySystem
{
    private static Dictionary<ulong, List<Item>> _invMap = [];

    public delegate void InventoryChangeEvent(ulong entity, Item item, bool added);

    public static InventoryChangeEvent InventoryChangeHandlers;

    public static void Register(ulong entity, List<Item> initialInventory)
    {
        _invMap.Add(entity, initialInventory);
    }

    public static void Remove(ulong entity)
    { 
        _invMap.Remove(entity); 
    }

    public static void Transfer(ulong fromEntity, ulong toEntity, Item itemToTransfer)
    {
        AddItem(toEntity, itemToTransfer);
        RemoveItem(fromEntity, itemToTransfer);
    }

    public static void AddItem(ulong entity, Item item)
    {
        _invMap[entity].Add(item);
        InventoryChangeHandlers?.Invoke(entity, item, true);
    }

    public static void RemoveItem(ulong entity, Item item)
    {
        _invMap[entity].Remove(item);
        InventoryChangeHandlers?.Invoke(entity, item, false);
    }

    public static List<Item> RetrieveInventory(ulong entity)
    {
        return _invMap[entity];
    }
}
