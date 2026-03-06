using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class SetDialogueEntry : DialogueAction
{
    [Export]
    public CharacterData InteractableCharacterData;

    [Export]
    public int EntryIndex;

    public override void Execute(Action onComplete)
    {
        if (CharacterSystem.GetInstance(InteractableCharacterData.ResourcePath) is InteractableCharacter characterInstance)
        {
            characterInstance.EntryPoint = EntryIndex;
        }

        onComplete?.Invoke();
    }
}

