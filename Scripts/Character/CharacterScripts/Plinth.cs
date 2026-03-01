using Godot;

public partial class Plinth : InteractableCharacter
{
    private bool _doneInitialDialogue = false;

    public override void _Ready()
    {
        base._Ready();
        _senseArea.BodyEntered += OnCharacterSensed;
    }

    public void OnCharacterSensed(Node2D node)
    {
        if (node is Player && !_doneInitialDialogue)
        {
            _doneInitialDialogue = true;
            DialogueSystem.StartDialogue(Dialogue, EntryPoint);
        }
    }
}
