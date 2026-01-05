using System;
using Godot;

[GlobalClass]
[Tool]
public partial class WalkCharsToPointsBlocking : DialogueAction
{
    [Export]
    public CharacterData[] Characters;

    [Export]
    public Vector2[] Points;

    public override void Execute(Action onComplete)
    {
        if (Characters.Length != Points.Length)
        {
            onComplete?.Invoke();
            return;
        }

        int remaining = Characters.Length;

        for (int i = 0; i < Characters.Length; i++)
        {
            var character = CharacterSystem.GetInstance(Characters[i].ResourcePath);
            character.WalkToPoint(Points[i], () =>
            {
                remaining--;
                if (remaining == 0)
                {
                    onComplete?.Invoke();
                }
            });
        }
    }
}