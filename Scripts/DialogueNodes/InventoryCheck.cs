using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Linq;

[Tool]
[GlobalClass]
public partial class InventoryCheck : DialogueCondition
{
    [Export]
    public Item CheckItem;

    [Export]
    public CharacterData TargetCharacter;

    public override bool Evaluate()
    {
        var inventory = InventorySystem.RetrieveInventory(TargetCharacter.ResourcePath);

        return inventory.Any(item => item.ResourcePath.Equals(CheckItem.ResourcePath));
    }
}
