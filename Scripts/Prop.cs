using Godot;
using System;

public partial class Prop : StaticBody2D
{
	AnimationPlayer _animationPlayer;

	private Vector2 _destination;
	private float _time;
	private Action _hoverCompletionCallback;

	private Sprite2D _sprite;

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_animationPlayer.AnimationFinished += OnAnimationFinished;
	}

	public void SetSprite(Texture2D sprite)
	{
		_sprite.Texture = sprite;
	}

	public void HoverToPoint(Vector2 destination, float time, Action onComplete)
	{
		_destination = destination;
		_time = time;
		_hoverCompletionCallback = () =>
		{
			CombatSystem.NavRegion.BakeNavigationPolygon();
			onComplete();
		};
		_animationPlayer.Play("takeoff");
	}

	private void OnAnimationFinished(StringName animationName)
	{
		switch (animationName)
		{
			case "takeoff":
				{
					_animationPlayer.Play("hover");
					var tween = GetTree().CreateTween();
					tween.TweenProperty(this, "position", _destination, _time);
					tween.TweenCallback(Callable.From(() => _animationPlayer.Play("land"))).SetDelay(1.0f);
					break;
				}
			case "land":
				{
					_hoverCompletionCallback?.Invoke();
					break;
				}
			default:
				{
					break;
				}
		}
	}
}
