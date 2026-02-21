using ArkhamHunters.Scripts;
using Godot;

[Tool]
[GlobalClass]
public partial class Ability : Resource
{
    [Export]
    public string AbilityName = "New Ability";
    [Export]
    public string Description = "Ability Description";
    [Export]
    public int Cooldown = 0;
    [Export]
    public float ProjectileSpeed = 150.0f;
    [Export]
    public DamageRoll ContactDamage;
    [Export]
    public DamageRoll AreaDamage;
    [Export]
    public Shape2D AreaShape;
    [Export]
    public Texture2D Icon;
    [Export]
    public PackedScene ProjectileScene;

    public void Activate(Character user, Character target, Vector2 SpawnPoint, Vector2 TargetPoint)
    {
        if (ProjectileScene != null)
        {
            Projectile projectileInstance = ProjectileScene.Instantiate<Projectile>();
            projectileInstance.Position = SpawnPoint;
            Vector2 direction = TargetPoint - user.Position;
            projectileInstance.Initialize(direction, target.GetInstanceId(), ProjectileSpeed);
            user.GetParent().AddChild(projectileInstance);
            projectileInstance.OnHit += () => OnProjectileHit(user, target, TargetPoint);
        }
        else
        {
            OnProjectileHit(user, target, TargetPoint);
        }
    }

    public void OnProjectileHit(Character user, Character target, Vector2 Position)
    {
        CombatSystem.AttemptAttack(user.CharacterData, target.CharacterData, ContactDamage);
        if (AreaShape != null)
        {
            // Apply area damage to characters within the area shape.
            var results = CombatSystem.GetCharactersInRange(Position, AreaShape);
            foreach (var result in results)
            {
                CombatSystem.AttemptAttack(user.CharacterData, result.CharacterData, AreaDamage);
            }
        }
    }
}