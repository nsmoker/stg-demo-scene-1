using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

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

    public override void Execute(Action onComplete)
    {
        InventorySystem.Transfer(FromCharacter.ResourcePath, ToCharacter.ResourcePath, ItemToTransfer);
        onComplete?.Invoke();
    }
}
