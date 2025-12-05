using System.Collections.Generic;
using ArkhamHunters.Scripts;

public static class CharacterSystem
{
    private static Dictionary<string, Character> _characterMap = [];

    public static void SetInstance(string characterId, Character instance)
    {
        _characterMap.Add(characterId, instance);
    }

    public static Character GetInstance(string id)
    {
        return _characterMap[id];
    }
}