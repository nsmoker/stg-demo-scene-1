using STGDemoScene1.Scripts.Resources;

namespace STGDemoScene1.Scripts.Systems;

public class CombatTimer
{
    public System.Action Timeout;
    public int TurnsRemaining;
    public CharacterData RelativeToCharacter;
    public ulong Id;
}
