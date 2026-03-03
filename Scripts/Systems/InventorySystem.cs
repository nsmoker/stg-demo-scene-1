using STGDemoScene1.Scripts.Items;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class InventorySystem
{
    private static readonly Dictionary<string, List<Item>> s_invMap = [];

    public delegate void InventoryChangeEvent(string entity, Item item, bool added);

    public static event InventoryChangeEvent InventoryChangeHandlers;

    public static void SetInventory(string entity, List<Item> initialInventory) => s_invMap.Add(entity, initialInventory);

    public static void Remove(string entity) => s_invMap.Remove(entity);

    public static void Transfer(string fromEntity, string toEntity, Item itemToTransfer)
    {
        AddItem(toEntity, itemToTransfer);
        RemoveItem(fromEntity, itemToTransfer);
    }

    public static void AddItem(string entity, Item item)
    {
        s_invMap[entity].Add(item);
        InventoryChangeHandlers?.Invoke(entity, item, true);
    }

    public static void RemoveItem(string entity, Item item)
    {
        _ = s_invMap[entity].Remove(item);
        InventoryChangeHandlers?.Invoke(entity, item, false);
    }

    public static List<Item> RetrieveInventory(string entity)
    {
        if (!s_invMap.TryGetValue(entity, out List<Item> value))
        {
            return [];
        }
        else
        {
            return [.. value];
        }
    }
}
