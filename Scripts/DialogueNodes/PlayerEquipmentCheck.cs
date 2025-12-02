using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Godot;
using System;

[Tool]
[GlobalClass]
public partial class PlayerEquipmentCheck : DialogueCondition
{
    [Export]
    public CharacterData CharacterToCheck;

    [Export]
    public ItemType SlotToCheck;

    [Export]
    public bool ShouldBeEmpty = true;

    public override bool Evaluate()
    {
        EquipmentSystem.RetrieveEquipment(CharacterToCheck.ResourcePath, out EquipmentSet characterEquipment);

        if (characterEquipment == null)
        {
            return ShouldBeEmpty;
        }
        return SlotToCheck switch
        {
            ItemType.Weapon => ShouldBeEmpty ? characterEquipment.Weapon.ItemType == ItemType.None
                                                 : characterEquipment.Weapon.ItemType != ItemType.None,
            ItemType.Wearable => ShouldBeEmpty ? characterEquipment.Helmet.ItemType == ItemType.None
                                                 : characterEquipment.Helmet.ItemType != ItemType.None,
            ItemType.Armor => ShouldBeEmpty ? characterEquipment.Armor.ItemType == ItemType.None
                                                 : characterEquipment.Armor.ItemType != ItemType.None,
            _ => false,
        };
    }
}
