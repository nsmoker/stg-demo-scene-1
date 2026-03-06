using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Systems;
using STGDemoScene1.Scripts.Triggers;
using System;

namespace STGDemoScene1.Scripts.Resources.Abilities;

[Tool]
[GlobalClass]
public partial class Ability : Resource
{
    [Export]
    public string AbilityName = "New Ability";
    [Export]
    public string Description = "Ability Description";
    [Export]
    public int Cooldown;
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
    public bool TargetingIsAnimated;

    public void Activate(Character user, Character target, Vector2 spawnPoint, Vector2 targetPoint, Action animationCallback)
    {
        if (ProjectileScene != null)
        {
            Projectile projectileInstance = ProjectileScene.Instantiate<Projectile>();
            Vector2 direction = targetPoint - user.GlobalPosition;
            projectileInstance.Initialize(direction, target?.GetInstanceId() ?? 0, ProjectileSpeed);
            user.GetParent().AddChild(projectileInstance);
            projectileInstance.GlobalPosition = spawnPoint;
            projectileInstance.OnHit += () => OnProjectileHit(user, target, targetPoint);
            projectileInstance.OnHit += animationCallback;
        }
        else
        {
            OnProjectileHit(user, target, targetPoint);
        }
    }

    protected virtual void OnProjectileHit(Character user, Character target, Vector2 position)
    {
        if (ContactDamage != null)
        {
            CombatSystem.AttemptAttack(user, target, ContactDamage);
        }

        if (AreaEffectScene == null)
        {
            return;
        }

        AreaEffect areaEffectInstance = AreaEffectScene.Instantiate<AreaEffect>();
        areaEffectInstance.Position = position;
        areaEffectInstance.SetCaster(user);
        areaEffectInstance.SetDamageRoll(AreaDamage);
        areaEffectInstance.SetDuration(AreaDuration);
        SceneSystem.GetMasterScene().AddChild(areaEffectInstance);
        CombatSystem.PassTurn(user);
    }
}

