using ArkhamHunters.Scripts;
using Godot;

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
            OnFinish = () => CombatSystem.AttemptAttack(user.CharacterData, target.CharacterData, ContactDamage)
        };
        target.AddPush(push);
    }
}
