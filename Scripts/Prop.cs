using Godot;
using System;

public partial class Prop : StaticBody2D
{
    private AnimationPlayer _animationPlayer;

    private Vector2 _destination;
    private float _time;
    private Action _hoverCompletionCallback;

    private Sprite2D _sprite;
    private Marker2D _seatMarker;

    // ONLY USE THIS TO REFERENCE THE NODE FROM A SCRIPT.
    [Export]
    private PropData _data;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _animationPlayer.AnimationFinished += OnAnimationFinished;
        _animationPlayer.Play("idle");
        _seatMarker = GetNodeOrNull<Marker2D>("SeatMarker");
        if (_data != null)
        {
            PropSystem.Register(_data, this);
        }
    }

    public void SetSprite(Texture2D sprite) => _sprite.Texture = sprite;

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
                    _ = tween.TweenProperty(this, "position", _destination, _time);
                    _ = tween.TweenCallback(Callable.From(() => _animationPlayer.Play("land"))).SetDelay(1.0f);
                    break;
                }
            case "land":
                {
                    _hoverCompletionCallback?.Invoke();
                    _animationPlayer.Play("idle");
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    public bool IsSeat() => _seatMarker != null;

    public bool IsNamedProp() => _data != null;

    public Vector2 GetSeatRegionCenter() => _seatMarker.GlobalPosition;
}
