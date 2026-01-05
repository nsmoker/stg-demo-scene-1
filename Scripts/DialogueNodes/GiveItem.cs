using System;
using Godot;

[GlobalClass]
[Tool]
public partial class GiveItem : DialogueAction
{
    [Export]
    CharacterData Recipient;

    [Export]
    Item ItemToGive;

    public override void Execute(Action onComplete)
    {
        InventorySystem.AddItem(Recipient.ResourcePath, ItemToGive);
        onComplete?.Invoke();
    }
}