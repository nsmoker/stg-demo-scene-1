using Godot;
using STGDemoScene1.Scripts.Resources.Abilities;
using STGDemoScene1.Scripts.Systems;
using Character = STGDemoScene1.Scripts.Characters.Character;

namespace STGDemoScene1.Scripts;

public partial class Targeting : Sprite2D
{
    private AnimationPlayer _anim;

    public Ability Ability;

    public Character Caster;

    public bool ShouldAnimate = false;

    public Texture2D Tex;

    public override void _Ready()
    {
        base._Ready();
        if (!ShouldAnimate)
        {
            Input.SetCustomMouseCursor(Tex);
            Visible = false;
            ProcessMode = ProcessModeEnum.Always;
        }
        else
        {
            _anim = GetNode<AnimationPlayer>("AnimationPlayer");
            _anim.Play("play");
            Input.MouseMode = Input.MouseModeEnum.Hidden;
        }
    }

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();

        if (Input.IsActionJustReleased("Targeting Interact"))
        {
            var pos = GetGlobalMousePosition();
            var hovered = CharacterSystem.GetInstance(HoverSystem.Hovered);
            var c = Caster;
            if (HoverSystem.AnyHovered() || Ability.ContactDamage == null)
            {
                c.BeginAttackAnim(
                    pos - c.GlobalPosition,
                    // Note: this lambda is evil and Godot will rightly punish us for trying to do things this way if we are not very, very cautious about the lifetimes of the objects here.
                    () => Ability.Activate(c, hovered, c.GetProjectileSpawnPoint(), pos, () => { })
                );

                SceneSystem.GetMasterScene().GetCombatController().OnAbilityTargetingEnd(Ability);
                Free();
            }
        }
        else if (Input.IsActionJustReleased("Targeting Back"))
        {
            SceneSystem.GetMasterScene().GetCombatController().OnAbilityTargetingEnd(Ability);
            Free();
        }
    }
}

