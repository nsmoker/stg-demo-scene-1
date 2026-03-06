using Godot;
using STGDemoScene1.Addons.Aeolus;
using STGDemoScene1.Scripts.AI;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts;

public partial class StagfootScreen : Node2D
{
    private Node2D _clearableProps;
    private Node2D _genericNpcRoot;
    private readonly List<Character> _genericNpcInstances = [];
    private Area2D _screenArea;

    private Sprite2D Backdrop { get; set; }
    public NavigationRegion2D NavRegion { get; private set; }

    [Export]
    public int GenericNpcCount;

    [Export]
    public Godot.Collections.Array<Texture2D> GenericNpcSprites = [];

    [Export]
    public PackedScene GenericNpcScene;

    [Export]
    public CrowdAiDirector GenericNpcDirector = new();

    [Export]
    public Godot.Collections.Array<FlowField> FlowFields = [];


    public Vector2 GetRandomTraversablePoint()
    {
        // Generate a random point within the axis aligned bounding box of the nav mesh.
        Rect2 navMeshAabb = NavRegion.GetBounds();
        Vector2 aabbTopLeft = navMeshAabb.Position;
        // We have to call .Abs because the Godot docs explicitly state that Rect2.Size is not always positive (?!?!?!?!?)
        Vector2 aabbExtent = navMeshAabb.Size.Abs();
        var random = new Random();
        float xVal = aabbTopLeft.X + aabbExtent.X * random.NextSingle();
        float yVal = aabbTopLeft.Y + aabbExtent.Y * random.NextSingle();
        var randomPoint = ToGlobal(new Vector2(xVal, yVal));

        // Constrain the AABB point to the nav mesh's surface.
        Vector2 surfacePoint = NavigationServer2D.RegionGetClosestPoint(NavRegion.GetRid(), randomPoint);
        return surfacePoint;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        NavRegion = GetNode<NavigationRegion2D>("NavigationRegion2D");
        Backdrop = GetNode<Sprite2D>("SceneBackdrop");
        SceneSystem.Register(SceneFilePath, this);
        _clearableProps = GetNode<Node2D>("ClearableProps");
        _screenArea = GetNode<Area2D>("ScreenArea");
        NavigationServer2D.MapChanged += OnFirstNavMeshSync;
    }

    public void OnSceneSystemReady()
    {
        // Trigger body entered for nodes already in the area.
        foreach (Node2D body in _screenArea.GetOverlappingBodies())
        {
            OnBodyEntered(body);
        }
        _screenArea.BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta) => GenericNpcDirector.Process(delta, this);

    private void OnFirstNavMeshSync(Rid mapId)
    {
        if (mapId.Equals(NavRegion.GetNavigationMap()) && GenericNpcCount > 0)
        {
            _genericNpcRoot = new Node2D
            {
                Name = "GenericNpcRoot",
                YSortEnabled = true
            };
            AddChild(_genericNpcRoot);
            var random = new Random();

            for (int i = 0; i < GenericNpcCount; ++i)
            {
                var npc = GenericNpcScene.Instantiate<Character>();
                int spriteIndex = random.Next(0, GenericNpcSprites.Count);

                _genericNpcRoot.AddChild(npc);
                npc.GlobalPosition = GetRandomTraversablePoint();
                npc.SetSprite(GenericNpcSprites[spriteIndex]);
                npc.SetCollisionOverride(false);
                _genericNpcInstances.Add(npc);
            }

            NavigationServer2D.MapChanged -= OnFirstNavMeshSync;
        }

        GenericNpcDirector.ManagedCharacters = _genericNpcInstances;
    }

    public void ClearNpcs()
    {
        GenericNpcDirector.ManagedCharacters = [];
        GenericNpcCount = 0;
        foreach (var npc in _genericNpcInstances)
        {
            npc.QueueFree();
        }
    }

    public void SetBackdropTexture(Texture2D texture) => Backdrop.Texture = texture;

    private IEnumerable<StaticBody2D> GetProps() => _clearableProps.FindChildren("*", type: "StaticBody2D", recursive: true).Cast<StaticBody2D>();

    public IEnumerable<StaticBody2D> GetUnnamedFurnitureProps() => GetProps().Where(x => x is Prop prop && prop.IsSeat() && !prop.IsNamedProp());

    public void DisableProps()
    {
        if (_clearableProps != null)
        {
            var children = GetProps();
            foreach (StaticBody2D child in children)
            {
                child.Visible = false;
                child.ProcessMode = ProcessModeEnum.Disabled;
                child.RemoveFromGroup("NavObjects");
            }
            CombatSystem.NavRegion.BakeNavigationPolygon();
        }
    }

    private void EnableProps()
    {
        if (_clearableProps != null)
        {
            var children = GetProps();
            foreach (StaticBody2D child in children)
            {
                child.Visible = true;
                child.ProcessMode = ProcessModeEnum.Inherit;
                child.AddToGroup("NavObjects");
            }
            CombatSystem.NavRegion.BakeNavigationPolygon();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player)
        {
            SceneSystem.GetMasterScene().SwitchScene(this, true);
        }
    }

    public bool CheckBounds(Vector2 position) => NavRegion.GetBounds().HasPoint(position);

    public Vector2 GetEdge() => NavRegion.GetBounds().Position + NavRegion.GetBounds().Size * new Vector2(0.0f, 0.5f);
}
