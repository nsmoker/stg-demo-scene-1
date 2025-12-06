using Godot;
using System;

[GlobalClass]
[Tool]
public partial class SetDialogueEntry : DialogueAction
{
	[Export]
	public CharacterData InteractableCharacterData;

	[Export]
	public int EntryIndex;

    public override void Execute()
    {
        var characterInstance = CharacterSystem.GetInstance(InteractableCharacterData.ResourcePath) as InteractableCharacter;
		characterInstance.EntryPoint = EntryIndex;
    }
}
