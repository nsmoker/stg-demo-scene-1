using Godot;

public partial class AbilityButton : Button
{
    private Ability _ability;

    public System.Action OnPressed;

    public override void _Ready()
    {
        base._Ready();
        Pressed += () => OnPressed?.Invoke();
    }

    public void SetAbility(Ability ability)
    {
        _ability = ability;
        Icon = ability.Icon;
    }

    public void SetIndex(int index)
    {
        Text = index.ToString();
    }
}