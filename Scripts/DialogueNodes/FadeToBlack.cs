using Godot;
using System;

[GlobalClass]
[Tool]
public partial class FadeToBlack : DialogueAction
{
    public override void Execute(Action onComplete)
    {
        var stagfootScreen = (Godot.Engine.GetMainLoop() as SceneTree)
           .CurrentScene as StagfootScreen;
        var tween = stagfootScreen.GetTree().CreateTween();
        tween
            .TweenProperty(stagfootScreen, "modulate", new Color(0.0f, 0.0f, 0.0f, 1.0f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        onComplete?.Invoke();
    }
}
