using System;
using Godot;

[GlobalClass]
[Tool]
public partial class RemoveItem : DialogueAction
{
    [Export]
    public CharacterData FromCharacter;

    [Export]
    public Item ItemToRemove;

    public override void Execute(Action onComplete)
    {
        InventorySystem.RemoveItem(FromCharacter.ResourcePath, ItemToRemove);
        onComplete?.Invoke();
    }
}