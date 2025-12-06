using Godot;

[GlobalClass]
[Tool]
public partial class GiveItem : DialogueAction
{
    [Export]
    CharacterData Recipient;

    [Export]
    Item ItemToGive;

    public override void Execute()
    {
        InventorySystem.AddItem(Recipient.ResourcePath, ItemToGive);
    }
}