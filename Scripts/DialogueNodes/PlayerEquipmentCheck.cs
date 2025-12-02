using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Godot;
using System;

[Tool]
[GlobalClass]
public partial class PlayerEquipmentCheck : DialogueCondition
{
    [Export]
    public ItemType SlotToCheck;

    [Export]
    public bool ShouldBeEmpty = true;

    public override bool Evaluate()
    {
        EquipmentSet playerEquipment = EquipmentSystem.GetPlayerEquipment();

        return SlotToCheck switch
        {
            ItemType.Weapon => ShouldBeEmpty ? playerEquipment.Weapon.ItemType == ArkhamHunters.Scripts.Items.ItemType.None
                                                 : playerEquipment.Weapon.ItemType != ArkhamHunters.Scripts.Items.ItemType.None,
            ItemType.Wearable => ShouldBeEmpty ? playerEquipment.Helmet.ItemType == ArkhamHunters.Scripts.Items.ItemType.None
                                                 : playerEquipment.Helmet.ItemType != ArkhamHunters.Scripts.Items.ItemType.None,
            ItemType.Armor => ShouldBeEmpty ? playerEquipment.Armor.ItemType == ArkhamHunters.Scripts.Items.ItemType.None
                                                 : playerEquipment.Armor.ItemType != ArkhamHunters.Scripts.Items.ItemType.None,
            _ => false,
        };
    }
}
