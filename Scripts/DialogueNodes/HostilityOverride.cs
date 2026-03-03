using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class HostilityOverride : DialogueAction
{
    [Export]
    public CharacterData Hater;

    [Export]
    public CharacterData Hatee;

    [Export]
    public bool IsHostile;

    public override void Execute(Action onComplete)
    {
        HostilitySystem.SetHostilityOverride(Hater.ResourcePath, Hatee.ResourcePath, IsHostile);
        onComplete?.Invoke();
    }
}
