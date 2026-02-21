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
    public int AreaDuration;
    [Export]
    public Texture2D Icon;
    [Export]
    public PackedScene ProjectileScene;
    [Export]
    public PackedScene AreaEffectScene;
    [Export]
    public PackedScene TargetingScene;
    [Export]
    public Texture2D TargetingSprite;
    [Export]
    public bool TargetingIsAnimated = false;

    public void Activate(Character user, Character target, Vector2 SpawnPoint, Vector2 TargetPoint)
    {
        if (ProjectileScene != null)
        {
            Projectile projectileInstance = ProjectileScene.Instantiate<Projectile>();
            projectileInstance.GlobalPosition = SpawnPoint;
            Vector2 direction = TargetPoint - user.GlobalPosition;
            projectileInstance.Initialize(direction, target != null ? target.GetInstanceId() : 0, ProjectileSpeed);
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
        if (ContactDamage != null)
        {
            CombatSystem.AttemptAttack(user.CharacterData, target.CharacterData, ContactDamage);
        }

        if (AreaEffectScene != null)
        {
            AreaEffect areaEffectInstance = AreaEffectScene.Instantiate<AreaEffect>();
            areaEffectInstance.Position = Position;
            areaEffectInstance.BodyEntered += (body) =>
            {
                if (body is Character)
                {
                    HealthSystem.PostDamageEvent(user, target, AreaDamage.Roll());
                }
            };
            areaEffectInstance.SetCooldown(AreaDuration);
            areaEffectInstance.SetCaster(user);
            areaEffectInstance.SetDamageRoll(AreaDamage);
            SceneSystem.GetMasterScene().AddChild(areaEffectInstance);
            CombatSystem.PassTurn(user.CharacterData);
        }
    }
}