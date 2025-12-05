using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;

public static class EquipmentSystem
{
    private static Dictionary<string, EquipmentSet> _equipmentMap = [];

    public delegate void EquipmentChangeEvent(string id, EquipmentSet equipmentSet);

    public static EquipmentChangeEvent EquipmentChangeHandlers;

    public static void SetEquipment(string id, EquipmentSet equipmentSet)
    {
        _equipmentMap[id] = equipmentSet;

        EquipmentChangeHandlers?.Invoke(id, equipmentSet);
    }

    public static bool RetrieveEquipment(string id, out EquipmentSet equipmentSet)
    {
        return _equipmentMap.TryGetValue(id, out equipmentSet);
    }

    public static void RemoveEquipment(string id)
    {
        _equipmentMap.Remove(id);
    }
}
