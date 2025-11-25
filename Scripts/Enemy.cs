using ArkhamHunters.Scripts.Abilities;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class Enemy : Character
{
    [Export] public float Speed = 300.0f;

    [Export] public Godot.Collections.Array<PatrolLeg> PatrolLegs;

    private int _patrolLegIndex;
    private double _patrolLegProgress = 0;

    private Area2D _senseArea;

    private CollisionShape2D _collisionShape;

    private AbilityMenu _combatInteractionMenu;

    private class CombatState : ICharacterState
    {
        public void Process(double delta, Character character)
        {
            var enemy = (Enemy)character;

        }

        public void PhysicsProcess(double delta, Character character)
        {
        }
    }

    private class PatrolState : ICharacterState
    {
        public void Process(double delta, Character character)
        {
            var enemy = (Enemy)character;
            var currentPatrolLeg = enemy.PatrolLegs[enemy._patrolLegIndex];
            if (currentPatrolLeg.Direction.X < 0)
            {
                enemy.SpriteAnim.Play("walk_west");
            }

            if (currentPatrolLeg.Direction.X > 0)
            {
                enemy.SpriteAnim.Play("walk_east");
            }

            if (currentPatrolLeg.Direction.Y > 0)
            {
                enemy.SpriteAnim.Play("walk_north");
            }

            if (currentPatrolLeg.Direction.Y < 0)
            {
                enemy.SpriteAnim.Play("walk_south");
            }

            foreach (var body in enemy._senseArea.GetOverlappingBodies())
            {
                if (body is Player player)
                {
                    enemy.State = new CombatState();
                    return;
                }
            }
        }

        public void PhysicsProcess(double delta, Character character)
        {
            var enemy = (Enemy)character;
            var currentPatrolLeg = enemy.PatrolLegs[enemy._patrolLegIndex];
            if (enemy._patrolLegProgress >= currentPatrolLeg.Distance)
            {
                enemy._patrolLegProgress = 0;
                enemy._patrolLegIndex = (enemy._patrolLegIndex + 1) % enemy.PatrolLegs.Count;
                currentPatrolLeg = enemy.PatrolLegs[enemy._patrolLegIndex];
            }

            enemy.Velocity = currentPatrolLeg.Direction * enemy.Speed;
            enemy._patrolLegProgress += enemy.Velocity.Length();
            enemy.MoveAndSlide();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        State = new PatrolState();
        _senseArea = GetNode<Area2D>("SenseArea");
        _combatInteractionMenu = GetNode<AbilityMenu>("CombatInteractionMenu");
        _combatInteractionMenu.Visible = false;
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
    }

    public AbilityMenu GetCombatInteractionMenu()
    {
        return _combatInteractionMenu;
    }
    
    public CollisionShape2D GetCollisionShape()
    {
        return _collisionShape;
    }
}