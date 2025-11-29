using ArkhamHunters.Scripts;
using Godot;
using System;

[Tool]
[GlobalClass]
public partial class ItemCheck : DialogueCondition
{
    public override bool Evaluate()
    {
        EquipmentSet playerEquipment = EquipmentSystem.GetPlayerEquipment();

        GD.Print(playerEquipment.Armor.Name);

        return playerEquipment.Armor.ItemType == ArkhamHunters.Scripts.Items.ItemType.None;
    }
}
