using ArkhamHunters.Scripts;
using Godot;
using System.Collections.Generic;

public partial class AbilityBar : HBoxContainer
{
    public List<AbilityButton> AbilityButtons = [];

    [Export]
    public PackedScene AbilityButtonScene;

    public void ShowForCharacter(Character character)
    {
        Visible = true;
        ClearButtons();

        for (int i = 0; i < character.Abilities.Count; i++)
        {
            var ability = character.Abilities[i];
            var button = AbilityButtonScene.Instantiate<AbilityButton>();
            AddChild(button);
            button.character = character;
            button.SetAbility(ability);
            button.SetIndex(AbilityButtons.Count + 1);
            AbilityButtons.Add(button);
        }
    }

    public void ClearButtons()
    {
        foreach (var button in AbilityButtons)
        {
            button.QueueFree();
        }
        AbilityButtons.Clear();
    }
}
