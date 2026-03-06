using Godot;
using STGDemoScene1.Scripts.Characters;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Controls;

public partial class AbilityBar : HBoxContainer
{
    private readonly List<AbilityButton> _abilityButtons = [];

    [Export]
    public PackedScene AbilityButtonScene;

    public void ShowForCharacter(Character character)
    {
        Visible = true;
        ClearButtons();

        foreach (var ability in character.Abilities)
        {
            var button = AbilityButtonScene.Instantiate<AbilityButton>();
            AddChild(button);
            button.Character = character;
            button.SetAbility(ability);
            button.SetIndex(_abilityButtons.Count + 1);
            _abilityButtons.Add(button);
        }
    }

    private void ClearButtons()
    {
        foreach (var button in _abilityButtons)
        {
            button.QueueFree();
        }
        _abilityButtons.Clear();
    }
}

