using System;
using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class ElementalStudiesDuelTransition : DialogueAction
{

    [Export]
    public Vector2 MarotSpawnPosition;

    [Export]
    public PackedScene MarotScene;

    [Export]
    public CharacterData DarianData;

    [Export]
    public CharacterData PlayerData;

    public override void Execute(Action onComplete)
    {
        var masterScene = (Godot.Engine.GetMainLoop() as SceneTree)
			.CurrentScene as MasterScene;
        StagfootScreen stagfootScreen = masterScene.GetCurrentScreen();
		var tween = stagfootScreen.GetTree().CreateTween();
		tween
			.TweenProperty(masterScene, "modulate", new Color(0.0f, 0.0f, 0.0f, 1.0f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(stagfootScreen.DisableProps));
        tween.TweenCallback(Callable.From(() =>
        {
            var marot = MarotScene.Instantiate<Character>();
            marot.GlobalPosition = MarotSpawnPosition;
            stagfootScreen.AddChild(marot);
            CharacterSystem.Despawn(DarianData);
            var player = CharacterSystem.GetInstance(PlayerData.ResourcePath);
            player.SetAnimState(Character.AnimState.Idle);
        }));
		tween
			.TweenProperty(masterScene, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(onComplete));
		tween.Play();
    }
}