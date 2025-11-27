using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Abilities;
using Godot;
using System;

public struct AbilityUseEvent
{
    public Ability ability;
    public Character attacker;
    public Character target;
    public bool hit;
    // Only use if hit is true.
    public int targetDamage;
    public int areaDamage;
}

public static class CombatSystem
{
    public delegate void AbilityEventHandler(AbilityUseEvent e);

    public static AbilityEventHandler abilityEventHandler;

    // Attempt to use an ability. Only call this function from PhysicsProcess.
    public static void UseAbility(Ability ability, Character attacker, Character target)
    {
        var rand = new Random();
        var distance = (attacker.GetClosestOnCollSurface(target.Position) - target.GetClosestOnCollSurface(attacker.Position)).Length();
        AbilityUseEvent ret = new();
        ret.ability = ability;
        ret.target = target;
        ret.attacker = attacker;
        // First, check if the ability hit.
        // If the target's out of range, it definitely didn't hit.
        if (distance > ability.Range)
        {
            ret.hit = false;
        }
        // If it's in range, we might first need to roll to hit.
        else if (ability.RollsToHit)
        {
            var roll = rand.Next(20) + 1 + ability.ToHitMod + attacker.ComputeToHitMod();
            ret.hit = roll > target.ComputeAc();
        }
        // If there's no roll necessary and the ability is a range, it's definitely a hit.
        else
        {
            ret.hit = true;
        }

        // If the ability hit, we need to compute damage and healing.
        if (ret.hit)
        {
            foreach (var roll in ability.TargetDamage.Rolls)
            {
                var rollResult = rand.Next(roll) + 1 + ability.TargetDamage.Mod;
                ret.targetDamage += rollResult;
            }

            foreach (var roll in ability.AreaDamage.Rolls)
            {
                var rollResult = rand.Next(roll) + 1 + ability.AreaDamage.Mod;
                ret.areaDamage += rollResult;
            }

            foreach (var roll in ability.TargetHealing.Rolls)
            {
                var rollResult = rand.Next(roll) + 1 + ability.TargetHealing.Mod;
                ret.targetDamage -= rollResult;
            }

            foreach (var roll in ability.AreaHealing.Rolls)
            { 
                var rollResult = rand.Next(roll) + 1 + ability.AreaHealing.Mod;
                ret.areaDamage -= rollResult;
            }

            // Send our event.
            abilityEventHandler?.Invoke(ret);

            // Post target damage event.
            HealthSystem.PostDamageEvent(attacker, target, ret.targetDamage, ability);

            // Check the area for collisions.
            var areaShape = new CircleShape2D();
            areaShape.Radius = ability.AreaRadius;
            var coll = new CollisionShape2D();
            coll.Shape = areaShape;
            coll.Position = target.Position;
            PhysicsDirectSpaceState2D spaceState = target.GetWorld2D().DirectSpaceState;
            var queryParams = new PhysicsShapeQueryParameters2D
            {
                CollideWithAreas = false,
                CollideWithBodies = true,
                Exclude = [],
                Shape = areaShape
            };
            var intersections = spaceState.IntersectShape(queryParams);

            foreach (var intersection in intersections)
            {
                var collided = (CollisionObject2D) intersection["collider"].AsGodotObject();
                if (collided is Character character)
                {
                    HealthSystem.PostDamageEvent(attacker, character, ret.areaDamage, ability);
                }
            }
        }
    }
}
