using Godot;
using System;

[GlobalClass]
[Tool]
public partial class GiveItem : DialogueAction
{
    [Export]
    private CharacterData Recipient;

    [Export]
    private Item ItemToGive;

    public override void Execute(Action onComplete)
    {
        InventorySystem.AddItem(Recipient.ResourcePath, ItemToGive);
        onComplete?.Invoke();
    }
}
