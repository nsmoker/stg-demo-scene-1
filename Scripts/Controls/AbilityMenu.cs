using Godot;

namespace STGDemoScene1.Scripts.Controls;

public partial class AbilityMenu : Control
{
    private TextureButton _activationButton;

    private int _currentIndex;

    public override void _Ready()
    {
        base._Ready();
        _activationButton = GetNode<TextureButton>("ActivationButton");
        _activationButton.ButtonDown += OnActivationButtonPressed;
    }

    private void OnActivationButtonPressed()
    {

    }
}
