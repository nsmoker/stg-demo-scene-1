
using Godot;
using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.Triggers;

public partial class Projectile : Area2D
{
    [Export]
    public float MaxLifetime = 5f;

    public Vector2 Velocity;

    public System.Action OnHit;

    public ulong TargetInstanceId;

    private float _timeAlive;

    public override void _Ready() => BodyEntered += OnBodyEntered;

    public override void _Process(double delta)
    {
        float deltaTime = (float) delta;
        Position += Velocity * deltaTime;
        _timeAlive += deltaTime;

        if (_timeAlive >= MaxLifetime)
        {
            QueueFree();
        }
    }

    public void Initialize(Vector2 direction, ulong targetInstanceId, float speed)
    {
        direction = direction.Normalized();
        Rotation = direction.Angle();
        Velocity = direction * speed;
        TargetInstanceId = targetInstanceId;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is Character character && character.GetInstanceId() == TargetInstanceId)
        {
            OnHit?.Invoke();
            QueueFree();
        }
    }
}

