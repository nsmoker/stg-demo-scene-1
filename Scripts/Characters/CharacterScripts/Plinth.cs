using Godot;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Characters.CharacterScripts;

public partial class Plinth : InteractableCharacter
{
    private bool _doneInitialDialogue = false;

    public override void _Ready()
    {
        base._Ready();
        SenseArea.BodyEntered += OnCharacterSensed;
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

