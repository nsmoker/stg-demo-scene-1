using Godot;
using System;
using System.Linq;

[Tool]
[GlobalClass]
public partial class InventoryCheck : DialogueCondition
{
    [Export]
    public Item CheckItem;

    public override bool Evaluate()
    {
        var player = GetPlayerNode();

        var playerInv = InventorySystem.RetrieveInventory(player.GetInstanceId());

        return playerInv.Any(item => item.Name.Equals(CheckItem.Name));
    }
}
