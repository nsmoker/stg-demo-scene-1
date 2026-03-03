using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class WalkCharToPointNonBlocking : DialogueAction
{
    [Export]
    public CharacterData CharacterData;

    [Export]
    public Vector2 Point;

    [Export]
    public Vector2 Facing;

    public override void Execute(Action onComplete)
    {
        var character = CharacterSystem.GetInstance(CharacterData.ResourcePath);
        character.WalkToPoint(Point, () => character.SetFacing(Facing));
        onComplete?.Invoke();
    }
}
