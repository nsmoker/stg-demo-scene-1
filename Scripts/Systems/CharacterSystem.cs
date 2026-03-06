using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class CharacterSystem
{
    private static readonly Dictionary<string, Character> s_characterMap = [];

    public static void SetInstance(CharacterData character, Character instance) => s_characterMap.Add(character.ResourcePath, instance);

    public static Character GetInstance(CharacterData character)
    {
        if (character == null)
        {
            return null;
        }
        return s_characterMap[character.ResourcePath];
    }

    public static Character GetInstance(string path) => s_characterMap[path];

    public static void Despawn(CharacterData character) => GetInstance(character).Despawn();
}

