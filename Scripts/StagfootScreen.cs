using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StagfootScreen : Node2D
{
	private Sprite2D _backdrop;
	private NavigationRegion2D _navRegion;
	private Node2D _clearableProps;
	public Sprite2D Backdrop { get => _backdrop; set => _backdrop = value; }
	public NavigationRegion2D NavRegion { get => _navRegion; set => _navRegion = value; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		NavRegion = GetNode<NavigationRegion2D>("NavigationRegion2D");
		Backdrop = GetNode<Sprite2D>("SceneBackdrop");
		SceneSystem.Register(SceneFilePath, this);
		_clearableProps = GetNodeOrNull<Node2D>("ClearableProps");
	}

	public void SetBackdropTexture(Texture2D texture)
	{
		Backdrop.Texture = texture;
	}

	public void DisableProps()
	{
		if (_clearableProps != null)
		{
            var children = _clearableProps.FindChildren("*", type: "StaticBody2D", recursive: true).Cast<Node2D>();
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
            var children = _clearableProps.FindChildren("*", type: "StaticBody2D", recursive: true).Cast<Node2D>();
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
