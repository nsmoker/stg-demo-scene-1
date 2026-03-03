using STGDemoScene1.Scripts.Resources;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class EquipmentSystem
{
    private static readonly Dictionary<string, EquipmentSet> s_equipmentMap = [];

    public delegate void EquipmentChangeEvent(string id, EquipmentSet equipmentSet);

    public static event EquipmentChangeEvent EquipmentChangeHandlers;

    public static void SetEquipment(string id, EquipmentSet equipmentSet)
    {
        s_equipmentMap[id] = equipmentSet;

        EquipmentChangeHandlers?.Invoke(id, equipmentSet);
    }

    public static bool RetrieveEquipment(string id, out EquipmentSet equipmentSet) => s_equipmentMap.TryGetValue(id, out equipmentSet);

    public static void RemoveEquipment(string id) => s_equipmentMap.Remove(id);
}
