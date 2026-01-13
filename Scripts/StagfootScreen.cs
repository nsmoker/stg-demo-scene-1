using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StagfootScreen : Node2D
{
	private Sprite2D _backdrop;
	private NavigationRegion2D _navRegion;
	private Node2D _clearableProps;
	private Node2D _genericNpcRoot;

	private List<Character> _genericNpcInstances = [];

	public Sprite2D Backdrop { get => _backdrop; set => _backdrop = value; }
	public NavigationRegion2D NavRegion { get => _navRegion; set => _navRegion = value; }

	[Export]
	public int GenericNpcCount = 0;

	[Export]
	public Godot.Collections.Array<Texture2D> GenericNpcSprites = [];

	[Export]
	public PackedScene GenericNpcScene;

	[Export]
	public CrowdAIDirector GenericNpcDirector = new();

	public Vector2 GetRandomTraversablePoint()
	{
		// Generate a random point within the axis aligned bounding box of the nav mesh.
		Rect2 navMeshAABB = _navRegion.GetBounds();
		Vector2 aabbTopLeft = navMeshAABB.Position;
		// We have to call .Abs because the Godot docs explicitly state that Rect2.Size is not always positive (?!?!?!?!?)
		Vector2 aabbExtent = navMeshAABB.Size.Abs();
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
		_clearableProps = GetNodeOrNull<Node2D>("ClearableProps");
		NavigationServer2D.MapChanged += OnFirstNavMeshSync;
	}

    public override void _Process(double delta)
    {
		GenericNpcDirector.Process(delta, this);
    }

	private void OnFirstNavMeshSync(Rid mapId)
	{
        if (mapId.Equals(_navRegion.GetNavigationMap()) && GenericNpcCount > 0)
        {
            _genericNpcRoot = new Node2D
            {
                Name = "GenericNpcRoot"
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

	public void SetBackdropTexture(Texture2D texture)
	{
		Backdrop.Texture = texture;
	}

	public IEnumerable<StaticBody2D> GetProps()
	{
		return _clearableProps.FindChildren("*", type: "StaticBody2D", recursive: true).Cast<StaticBody2D>();
	}

	public IEnumerable<StaticBody2D> GetUnnamedFurnitureProps()
	{
		return GetProps().Where(x => x is Prop prop && prop.IsSeat() && !prop.IsNamedProp());
	}

	public void DisableProps()
	{
		if (_clearableProps != null)
		{
            var children = GetProps();
            foreach (Node2D child in children)
			{
				child.Visible = false;
				child.ProcessMode = ProcessModeEnum.Disabled;
				child.RemoveFromGroup("NavObjects");
			}
			CombatSystem.NavRegion.BakeNavigationPolygon();
		}
    }

    public void EnableProps()
	{
		if (_clearableProps != null)
		{
            var children = GetProps();
            foreach (Node2D child in children)
            {
				child.Visible = true;
				child.ProcessMode = ProcessModeEnum.Inherit;
				child.AddToGroup("NavObjects");
			}
			CombatSystem.NavRegion.BakeNavigationPolygon();
		}
    }
}
