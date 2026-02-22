using ArkhamHunters.Scripts;
using Godot;

public partial class Vortex : Ability
{
    [Export]
    public StatusEffect SlowEffect;
    public override void OnProjectileHit(Character user, Character target, Vector2 Position)
    {
        if (AreaEffectScene != null)
        {
            AreaEffect areaEffectInstance = AreaEffectScene.Instantiate<AreaEffect>();
            areaEffectInstance.Position = Position;
            areaEffectInstance.SetCaster(user);
            areaEffectInstance.SetDamageRoll(AreaDamage);
            areaEffectInstance.SetDuration(AreaDuration);
            areaEffectInstance.ApplyToCharacter = (ch) =>
            {
                ch.AddStatusEffect(SlowEffect);
            };
            SceneSystem.GetMasterScene().AddChild(areaEffectInstance);
            CombatSystem.PassTurn(user.CharacterData);
        }
    }
}