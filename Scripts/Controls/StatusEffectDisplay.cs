using Godot;
using STGDemoScene1.Scripts.StatusEffects;

namespace STGDemoScene1.Scripts.Controls;

public partial class StatusEffectDisplay : PanelContainer
{
    private TextureRect _icon;
    private Label _stacks;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("VBoxContainer/TextureRect");
        _stacks = GetNode<Label>("VBoxContainer/Label");
    }

    public void SetStatusEffect(StatusEffect statusEffect, int stacks)
    {
        _icon.Texture = statusEffect.Icon;
        _stacks.Text = stacks.ToString();
    }
}
