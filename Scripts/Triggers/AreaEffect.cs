using System.Collections.Generic;
using ArkhamHunters.Scripts;
using Godot;

public partial class AreaEffect : Area2D
{
    private AnimationPlayer _anim;
    private int Cooldown;
    private Character _caster;
    private DamageRoll _damageRoll;

    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
        _anim.Play("play");
        CombatSystem.TurnHandlers += OnTurnBegin;
        foreach (var side in CombatSystem.GetSides())
        {
            DealAreaDamage(side);
        }
    }

    public void PlayEndAnimation()
    {
        _anim.Play("end");
    }

    public void SetCooldown(int cooldown)
    {
        Cooldown = cooldown;
    }

    public void SetCaster(Character caster)
    {
        _caster = caster;
    }

    public void SetDamageRoll(DamageRoll damageRoll)
    {
        _damageRoll = damageRoll;
    }


    public Character GetCaster()
    {
        return _caster;
    }

    public void OnTurnBegin(List<string> side)
    {
        if (side.Contains(_caster.CharacterData.ResourcePath))
        {
            if (Cooldown > 0)
            {
                Cooldown--;
            }
            else
            {
                CombatSystem.TurnHandlers -= OnTurnBegin;
                QueueFree();
            }
        }
        DealAreaDamage(side);
    }

    public void DealAreaDamage(List<string> movingSide)
    {
        foreach (var x in GetOverlappingBodies())
        {
            if (x is Character character && movingSide.Contains(character.CharacterData.ResourcePath))
            {
                HealthSystem.PostDamageEvent(_caster, character, _damageRoll.Roll());
            }
        }
    }
}