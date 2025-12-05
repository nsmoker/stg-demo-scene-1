using System.Collections.Generic;
using ArkhamHunters.Scripts.Abilities;
using Godot;

public partial class AbilityMenu : Control
{
    [Export]
    private Godot.Collections.Array<Ability> _abilities = new();

    private TextureButton _upButton;
    private TextureButton _downButton;
    public TextureButton _activationButton;

    private int _currentIndex = 0;

    public override void _Ready()
    {
        base._Ready();
        _upButton = GetNode<TextureButton>("UpButton");
        _downButton = GetNode<TextureButton>("DownButton");
        _activationButton = GetNode<TextureButton>("ActivationButton");
        UpdateButtons();

        _upButton.Pressed += OnUpButtonPressed;
        _downButton.Pressed += OnDownButtonPressed;
    }

    private void OnUpButtonPressed()
    {
        _currentIndex = (_currentIndex + 1 + _abilities.Count) % _abilities.Count;
        UpdateButtons();
    }

    private void OnDownButtonPressed()
    {
        _currentIndex = (_currentIndex - 1 + _abilities.Count) % _abilities.Count;
        UpdateButtons();
    }

    public void SetAbilities(Godot.Collections.Array<Ability> abilities)
    {
        _abilities = abilities;
        _currentIndex = 0;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _upButton.Disabled = _abilities.Count == 0;
        _downButton.Disabled = _abilities.Count == 0;

        var currentAbility = _abilities.Count > 0 ? _abilities[_currentIndex] : null;
        if (currentAbility != null)
        {
            _activationButton.TextureNormal = currentAbility.Icon;
            _activationButton.TextureFocused = currentAbility.OutlineIcon;
            _activationButton.TextureHover = currentAbility.OutlineIcon;
            _activationButton.TexturePressed = currentAbility.OutlineIcon;
        }
    }

    public Ability GetCurrentAbility()
    {
        return _abilities.Count > 0 ? _abilities[_currentIndex] : null;
    }
}