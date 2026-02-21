using System.Collections.Generic;
using ArkhamHunters.Scripts;
using Godot;

public partial class AreaEffect : Area2D
{
    private AnimationPlayer _anim;
    private int Cooldown;
    private Character _caster;
    private DamageRoll _damageRoll;
    private Shape2D _shape;

    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
        _shape = GetNode<CollisionShape2D>("CollisionShape2D").Shape;
        _anim.Play("play");
        CombatSystem.TurnHandlers += OnTurnBegin;
        foreach (var side in CombatSystem.GetSides())
        {
            DealAreaDamage(side, true);
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

    public void DealAreaDamage(List<string> movingSide, bool manualShapecast = false)
    {
        // Due to a bug in Godot's area initialization, we cannot rely on GetOverlappingBodies or signals to 
        // report intersections with bodies that start inside the area, even if we wait for the next physics update.
        // As such, we have to do a manual shapecast at initialization.
        if (manualShapecast)
        {
            PhysicsDirectSpaceState2D physicsState = GetWorld2D().DirectSpaceState;
            PhysicsShapeQueryParameters2D physicsShapeQueryParameters2D = new()
            {
                Shape = _shape,
                Transform = Transform
            };
            foreach (var result in physicsState.IntersectShape(physicsShapeQueryParameters2D))
            {
                var character = result["collider"].As<Character>();
                if (character != null && movingSide.Contains(character.CharacterData.ResourcePath))
                {
                    HealthSystem.PostDamageEvent(_caster, character, _damageRoll.Roll());
                }
            }
        }
        else
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
}