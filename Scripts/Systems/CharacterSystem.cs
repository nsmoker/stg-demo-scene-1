using ArkhamHunters.Scripts;
using System.Collections.Generic;

public static class CharacterSystem
{
    private static readonly Dictionary<string, Character> _characterMap = [];

    public static void SetInstance(string characterId, Character instance) => _characterMap.Add(characterId, instance);

    public static Character GetInstance(string id)
    {
        if (id == null)
        {
            return null;
        }
        return _characterMap[id];
    }

    public static void Despawn(CharacterData character) => GetInstance(character.ResourcePath).Despawn();
}
