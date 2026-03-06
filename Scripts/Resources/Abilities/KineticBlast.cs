using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Resources.Abilities;

[Tool]
[GlobalClass]
public partial class KineticBlast : Ability
{

    [Export]
    public float PushStrength;
    [Export]
    public float PushDuration;

    public override void OnProjectileHit(Character user, Character target, Vector2 Position)
    {
        Push push = new()
        {
            Duration = PushDuration,
            Velocity = (Position - user.GlobalPosition).Normalized() * PushStrength,
            OnFinish = () => CombatSystem.AttemptAttack(user, target, ContactDamage)
        };
        target.AddPush(push);
    }
}

