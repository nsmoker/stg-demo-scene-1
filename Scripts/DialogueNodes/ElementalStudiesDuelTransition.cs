using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

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
        StagfootScreen stagfootScreen = SceneSystem.GetMasterScene().GetCurrentScreen();
        var tween = stagfootScreen.GetTree().CreateTween();
        _ = tween
            .TweenProperty(SceneSystem.GetMasterScene(), "modulate", new Color(0.0f, 0.0f, 0.0f, 1.0f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _ = tween.TweenCallback(Callable.From(stagfootScreen.DisableProps));
        _ = tween.TweenCallback(Callable.From(stagfootScreen.ClearNpcs));
        _ = tween.TweenCallback(Callable.From(() =>
        {
            var marot = MarotScene.Instantiate<Character>();
            marot.GlobalPosition = MarotSpawnPosition;
            stagfootScreen.AddChild(marot);
            CharacterSystem.Despawn(DarianData);
            var player = CharacterSystem.GetInstance(PlayerData.ResourcePath);
            player.SetAnimState(Character.AnimState.Idle);
        }));
        _ = tween
            .TweenProperty(SceneSystem.GetMasterScene(), "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _ = tween.TweenCallback(Callable.From(onComplete));
        tween.Play();
    }
}

