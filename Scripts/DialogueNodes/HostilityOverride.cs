using ArkhamHunters.Scripts;
using Godot;
using System;

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

    public override void Execute()
    {
        HostilitySystem.SetHostilityOverride(Hater.ResourcePath, Hatee.ResourcePath, IsHostile);
    }
}
