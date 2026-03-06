using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class SetScreenBackdrop : DialogueAction
{
    [Export]
    public Texture2D Texture;

    public override void Execute(Action onComplete)
    {
        if ((Engine.GetMainLoop() as SceneTree)?.CurrentScene is not StagfootScreen stagfootScreen)
        {
            return;
        }

        var tween = stagfootScreen.GetTree().CreateTween();
        _ = tween.TweenCallback(Callable.From(stagfootScreen.DisableProps));
        _ = tween
            .TweenProperty(stagfootScreen, "modulate", new Color(0.0f, 0.0f, 0.0f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _ = tween.TweenCallback(Callable.From(() => stagfootScreen.SetBackdropTexture(Texture)));
        _ = tween
            .TweenProperty(stagfootScreen, "modulate", new Color(1.0f, 1.0f, 1.0f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _ = tween.TweenCallback(Callable.From(onComplete));
        tween.Play();
    }
}
