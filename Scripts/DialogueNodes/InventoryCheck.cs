using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System.Linq;

namespace STGDemoScene1.Scripts.DialogueNodes;

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
