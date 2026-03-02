using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Controls;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts;

public partial class MasterScene : Node2D
{

    private JournalDisplay _journalDisplay;
    private Button _exitButton;
    private PanelContainer _exitMenu;
    private Player _player;
    private AbilityBar _abilityBar;
    private StagfootScreen _currentScreen;
    private Label _combatStatusLabel;
    private CombatController _combatController;

    [Export]
    private PackedScene _startingScene;

    public override void _Ready()
    {
        base._Ready();
        _journalDisplay = GetNode<JournalDisplay>("Camera2D/JournalDisplay");
        _exitButton = GetNode<Button>("Camera2D/ExitMenu/VBoxContainer/ExitButton");
        _exitButton.Pressed += OnExitPressed;
        _exitMenu = GetNode<PanelContainer>("Camera2D/ExitMenu");
        _player = GetNode<Player>("Player");
        _combatController = GetNode<CombatController>("CombatController");
        _combatController.SetPlayer(_player);
        _abilityBar = GetNode<AbilityBar>("Camera2D/AbilityBar");
        _currentScreen = SceneSystem.GetInstance(_startingScene.ResourcePath);
        _combatStatusLabel = GetNode<Label>("Camera2D/CombatStatusLabel");
        SceneSystem.SetMasterScene(this);
        CombatSystem.Initialize();
        SwitchScene(_currentScreen, false);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Exit Menu"))
        {
            _ = ToggleExitMenu();
        }
    }

    public StagfootScreen GetCurrentScreen() => _currentScreen;

    public Label GetCombatStatusLabel() => _combatStatusLabel;

    public void SwitchScene(StagfootScreen destination, bool fade)
    {
        _currentScreen = destination;
        CombatSystem.NavRegion = destination.NavRegion;
        Camera2D camera = GetViewport().GetCamera2D();

        if (fade)
        {
            var tween = GetTree().CreateTween();
            _ = tween.TweenProperty(camera, "global_position", destination.GlobalPosition, 0.3f);
            _ = tween.SetEase(Tween.EaseType.InOut);
        }
        else
        {
            camera.GlobalPosition = destination.GlobalPosition;
        }
    }

    public bool ToggleJournalDisplay()
    {
        _journalDisplay.Visible = !_journalDisplay.Visible;
        return _journalDisplay.Visible;
    }

    public void SetJournalEntries(List<Quest> quests) => _journalDisplay.SetQuestEntries(quests);

    public bool ToggleExitMenu()
    {
        _exitMenu.Visible = !_exitMenu.Visible;
        return _exitMenu.Visible;
    }

    public void OnExitPressed() => GetTree().Quit();

    public void SetAbilityBarVisible(bool visible) => _abilityBar.Visible = visible;

    public void ActivateAbilityBarForCharacter(Character character)
    {
        _abilityBar.ShowForCharacter(character);
        SetAbilityBarVisible(true);
    }

    public void SetAbilityBarReceiveInput(bool receiveInput) => _abilityBar.ProcessMode = receiveInput ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;

    public Player GetPlayer() => _player;

    public CombatController GetCombatController() => _combatController;
}

