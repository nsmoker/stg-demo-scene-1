using Godot;
using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.StatusEffects;

[Tool]
[GlobalClass]
public partial class SlowEffect : StatusEffect
{
    public override bool OnStackAdd(Character target)
    {
        target.Speed *= 0.5f;
        target.MovementRange *= 0.5f;
        return true;
    }

    public override bool OnStackRemove(Character target)
    {
        target.Speed *= 2.0f;
        target.MovementRange *= 2.0f;
        return true;
    }
}

