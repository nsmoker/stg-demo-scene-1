using Godot;

[GlobalClass]
[Tool]
public partial class TransferItem : DialogueAction
{
    [Export]
    public CharacterData FromCharacter;

    [Export]
    public CharacterData ToCharacter;

    [Export]
    public Item ItemToTransfer;

    public override void Execute()
    {
        InventorySystem.Transfer(FromCharacter.ResourcePath, ToCharacter.ResourcePath, ItemToTransfer);
    }
}