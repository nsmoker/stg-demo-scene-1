using Godot;
using System;

[Tool]
[GlobalClass]
public partial class WalkCharToPointBlocking : DialogueAction
{
	[Export]
	public CharacterData CharacterData;

	[Export]
	public Vector2 Point;

    public override void Execute(Action onComplete)
    {
        var character = CharacterSystem.GetInstance(CharacterData.ResourcePath);
        character.WalkToPoint(Point, () => onComplete?.Invoke());
    }
}
