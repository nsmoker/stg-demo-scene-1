
using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Triggers;

public partial class AreaEffect : Area2D
{
    private AnimationPlayer _anim;
    private int _duration;
    private Character _caster;
    private DamageRoll _damageRoll;
    private Shape2D _shape;

    public System.Action<Character> ApplyToCharacter;

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
        _ = CombatSystem.CreateTimer(_duration, () =>
        {
            CombatSystem.TurnHandlers -= OnTurnBegin;
            QueueFree();
        }, _caster);
    }

    private void PlayEndAnimation() => _anim.Play("end");

    public void SetDuration(int duration) => _duration = duration;

    public void SetCaster(Character caster) => _caster = caster;

    public void SetDamageRoll(DamageRoll damageRoll) => _damageRoll = damageRoll;


    private Character GetCaster() => _caster;

    private void OnTurnBegin(List<Character> side) => DealAreaDamage(side);

    private void DealAreaDamage(List<Character> movingSide, bool manualShapecast = false)
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
                if (character != null && movingSide.Contains(character))
                {
                    HealthSystem.PostDamageEvent(_caster, character, _damageRoll.Roll());
                    ApplyToCharacter?.Invoke(character);
                }
            }
        }
        else
        {
            foreach (var x in GetOverlappingBodies())
            {
                if (x is Character character && movingSide.Contains(character))
                {
                    HealthSystem.PostDamageEvent(_caster, character, _damageRoll.Roll());
                    ApplyToCharacter?.Invoke(character);
                }
            }
        }
    }
}

