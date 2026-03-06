using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.StatusEffects;
using STGDemoScene1.Scripts.Systems;
using STGDemoScene1.Scripts.Triggers;

namespace STGDemoScene1.Scripts.Resources.Abilities;

public partial class Vortex : Ability
{
    [Export]
    public StatusEffect SlowEffect;

    protected override void OnProjectileHit(Character user, Character target, Vector2 position)
    {
        if (AreaEffectScene == null)
        {
            return;
        }

        AreaEffect areaEffectInstance = AreaEffectScene.Instantiate<AreaEffect>();
        areaEffectInstance.Position = position;
        areaEffectInstance.SetCaster(user);
        areaEffectInstance.SetDamageRoll(AreaDamage);
        areaEffectInstance.SetDuration(AreaDuration);
        areaEffectInstance.ApplyToCharacter = (ch) => ch.AddStatusEffect(SlowEffect);
        SceneSystem.GetMasterScene().AddChild(areaEffectInstance);
        CombatSystem.PassTurn(user);
    }
}

