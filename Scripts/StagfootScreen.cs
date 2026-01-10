using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StagfootScreen : Node2D
{
	private Sprite2D _backdrop;
	private JournalDisplay _journalDisplay;
	private Button _exitButton;
	private PanelContainer _exitMenu;
	public Sprite2D Backdrop { get => _backdrop; set => _backdrop = value; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Backdrop = GetNode<Sprite2D>("SceneBackdrop");
		_journalDisplay = GetNode<JournalDisplay>("JournalDisplay");
		_exitButton = GetNode<Button>("ExitMenu/VBoxContainer/ExitButton");
		_exitButton.Pressed += OnExitPressed;
		_exitMenu = GetNode<PanelContainer>("ExitMenu");
	}

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Exit Menu"))
		{
			ToggleExitMenu();
		}
    }

	public void SetBackdropTexture(Texture2D texture)
	{
		Backdrop.Texture = texture;
	}

	public Label GetCombatStatusLabel()
	{
		return GetNode<Label>("CombatStatusLabel");
	}

	public void DisableBackdropProps()
	{
		foreach (Node2D child in _backdrop.GetChildren().Cast<Node2D>())
		{
			child.Visible = false;
			child.ProcessMode = ProcessModeEnum.Disabled;
			child.RemoveFromGroup("NavObjects");
		}
        CombatSystem.NavRegion.BakeNavigationPolygon();
    }

    public void EnableBackdropProps()
	{
		foreach (Node2D child in _backdrop.GetChildren().Cast<Node2D>())
		{
			child.Visible = true;
			child.ProcessMode = ProcessModeEnum.Inherit;
			child.AddToGroup("NavObjects");
        }
        CombatSystem.NavRegion.BakeNavigationPolygon();
    }

	public bool ToggleJournalDisplay()
	{
		_journalDisplay.Visible = !_journalDisplay.Visible;
		return _journalDisplay.Visible;
	}

	public void SetJournalEntries(List<Quest> quests)
	{
		_journalDisplay.SetQuestEntries(quests);
	}

	public bool ToggleExitMenu()
	{
		_exitMenu.Visible = !_exitMenu.Visible;
		return _exitMenu.Visible;
	}

	public void OnExitPressed()
	{
		GetTree().Quit();
	}
}
