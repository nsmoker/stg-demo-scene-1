using System;
using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class ElementalStudiesDuelTransition : DialogueAction
{
    [Export]
	public Texture2D Texture;

    [Export]
    public Vector2 MarotSpawnPosition;

    [Export]
    public PackedScene MarotScene;

    [Export]
    public CharacterData DarianData;

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
        tween.TweenCallback(Callable.From(() =>
        {
            var marot = MarotScene.Instantiate<Character>();
            marot.GlobalPosition = MarotSpawnPosition;
            stagfootScreen.AddChild(marot);
            CharacterSystem.Despawn(DarianData);
        }));
		tween
			.TweenProperty(stagfootScreen, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(onComplete));
		tween.Play();
    }
}