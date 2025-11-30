using ArkhamHunters.Scripts;
using Godot;
using System;

[Tool]
[GlobalClass]
public partial class HostilityOverride : DialogueAction
{
    public override void Execute()
    {
        GD.Print("exec");
        Node root = ((SceneTree) Godot.Engine.GetMainLoop()).CurrentScene;
        var player = root.GetNode<Character>("Player");
        var sign = root.GetNode<Character>("NavigationRegion2D/Sign");
        HostilitySystem.SetHostilityOverride(sign.GetInstanceId(), player.GetInstanceId(), true);
    }
}
