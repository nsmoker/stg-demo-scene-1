using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.Systems;

public class CombatTimer
{
    public System.Action Timeout;
    public int TurnsRemaining;
    public Character RelativeTo;
    public ulong Id;
}
