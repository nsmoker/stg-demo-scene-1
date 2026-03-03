using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class GiveItem : DialogueAction
{
    [Export]
    private CharacterData _recipient;

    [Export]
    private Item _itemToGive;

    public override void Execute(Action onComplete)
    {
        InventorySystem.AddItem(_recipient.ResourcePath, _itemToGive);
        onComplete?.Invoke();
    }
}
