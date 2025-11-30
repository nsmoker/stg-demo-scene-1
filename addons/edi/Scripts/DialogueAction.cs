using ArkhamHunters.Scripts;
using Godot;
using System;

[Tool]
[GlobalClass]
public abstract partial class DialogueAction : Resource
{
    protected static Player GetPlayerNode()
    {
        Node root = ((SceneTree) Godot.Engine.GetMainLoop()).CurrentScene;
        return root.GetNode<Player>("Player");
    }

    public abstract void Execute();
}
