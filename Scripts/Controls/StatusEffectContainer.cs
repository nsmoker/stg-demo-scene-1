using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.StatusEffects;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Controls;

public partial class StatusEffectContainer : HBoxContainer
{
    private readonly List<StatusEffectDisplay> _statusEffectDisplays = [];

    [Export]
    public PackedScene StatusEffectDisplayScene;

    public void SetStatusEffects(Dictionary<StatusEffect, StackStatus> statusEffects)
    {
        foreach (var statusEffectDisplay in _statusEffectDisplays)
        {
            statusEffectDisplay.QueueFree();
        }

        _statusEffectDisplays.Clear();

        foreach (var statusEffect in statusEffects)
        {
            if (statusEffect.Value.NumStacks > 0)
            {
                var display = StatusEffectDisplayScene.Instantiate<StatusEffectDisplay>();
                AddChild(display);
                display.SetStatusEffect(statusEffect.Key, statusEffect.Value.NumStacks);
                _statusEffectDisplays.Add(display);
            }
        }
    }
}

