using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;

public static class EquipmentSystem
{
    private static ulong _playerId = 0;

    private static Dictionary<ulong, EquipmentSet> _equipmentMap = [];

    public delegate void EquipmentChangeEvent(ulong id, EquipmentSet equipmentSet);

    public static EquipmentChangeEvent EquipmentChangeHandlers;

    public static void SetEquipment(ulong id, EquipmentSet equipmentSet, bool isPlayer = false)
    {
        _equipmentMap[id] = equipmentSet;

        if (isPlayer)
        {
            _playerId = id;
        }

        EquipmentChangeHandlers(id, equipmentSet);
    }

    public static bool RetrieveEquipment(ulong id, out EquipmentSet equipmentSet)
    {
        return _equipmentMap.TryGetValue(id, out equipmentSet);
    }

    public static void RemoveEquipment(ulong id)
    {
        _equipmentMap.Remove(id);
    }

    public static EquipmentSet GetPlayerEquipment()
    {
        return _equipmentMap[_playerId];
    }
}
