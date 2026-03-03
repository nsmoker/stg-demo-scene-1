using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

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
