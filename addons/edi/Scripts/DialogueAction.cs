using Godot;
using STGDemoScene1.Scripts.Characters;
using System;

namespace STGDemoScene1.Addons.Edi.Scripts;

[Tool]
[GlobalClass]
public abstract partial class DialogueAction : Resource
{
    protected static Player GetPlayerNode()
    {
        Node root = ((SceneTree) Godot.Engine.GetMainLoop()).CurrentScene;
        return root.GetNode<Player>("Player");
    }

    public abstract void Execute(Action onComplete);
}
