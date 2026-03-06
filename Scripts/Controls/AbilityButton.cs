using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources.Abilities;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Controls;

public partial class AbilityButton : Button
{
    private Ability _ability;

    public Character Character;

    public override void _Ready()
    {
        base._Ready();
        Pressed += OnPressed;
    }

    public void SetAbility(Ability ability)
    {
        _ability = ability;
        Icon = ability.Icon;
    }

    public void SetIndex(int index)
    {
        Text = index.ToString();
        Shortcut = new Shortcut()
        {
            Events = [new InputEventKey() {
                Pressed = true,
                Keycode = Key.Key0 + index
            }]
        };
    }

    private void OnPressed()
    {
        if (_ability != null)
        {
            SceneSystem.GetMasterScene().GetCombatController().OnAbilityTargetingStart(_ability, Character);
        }
    }
}

