using Godot;
using System;
using System.Linq;

public partial class StagfootScreen : Node2D
{
	private Sprite2D _backdrop;
	public Sprite2D Backdrop { get => _backdrop; set => _backdrop = value; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Backdrop = GetNode<Sprite2D>("SceneBackdrop");
	}

	public void SetBackdropTexture(Texture2D texture)
	{
		Backdrop.Texture = texture;
	}

	public void DisableBackdropProps()
	{
		foreach (Node2D child in _backdrop.GetChildren().Cast<Node2D>())
		{
			child.Visible = false;
			child.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	public void EnableBackdropProps()
	{
		foreach (Node2D child in _backdrop.GetChildren().Cast<Node2D>())
		{
			child.Visible = true;
			child.ProcessMode = ProcessModeEnum.Inherit;
		}
	}
}
