using Godot;
using System;

[GlobalClass]
[Tool]
public partial class SetScreenBackdrop : DialogueAction
{
	[Export]
	public Texture2D Texture;

    public override void Execute(Action onComplete)
    {
        var stagfootScreen = (Godot.Engine.GetMainLoop() as SceneTree)
			.CurrentScene as StagfootScreen;
		var tween = stagfootScreen.GetTree().CreateTween();
		tween.TweenCallback(Callable.From(stagfootScreen.DisableBackdropProps));
		tween
			.TweenProperty(stagfootScreen, "modulate", new Color(0.0f, 0.0f, 0.0f, 1.0f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(() => stagfootScreen.SetBackdropTexture(Texture)));
		tween
			.TweenProperty(stagfootScreen, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(onComplete));
		tween.Play();
    }
}
