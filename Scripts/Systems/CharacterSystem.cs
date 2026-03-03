using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class CharacterSystem
{
    private static readonly Dictionary<string, Character> s_characterMap = [];

    public static void SetInstance(string characterId, Character instance) => s_characterMap.Add(characterId, instance);

    public static Character GetInstance(string id)
    {
        if (id == null)
        {
            return null;
        }
        return s_characterMap[id];
    }

    public static void Despawn(CharacterData character) => GetInstance(character.ResourcePath).Despawn();
}

