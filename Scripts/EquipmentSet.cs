using System;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts;

[GlobalClass]
public partial class EquipmentSet: Resource
{
    private Item _armor = Item.NoneItem();
    [Export]
    public Item Armor
    {
        get => _armor;
        set
        {
            _armor.Equipped = false;
            _armor = value;
            _armor.Equipped = true;
        }
    }

    private Item _weapon = Item.NoneItem();
    [Export]
    public Item Weapon
    {
        get => _weapon;
        set
        {
            _weapon.Equipped = false;
            _weapon = value;
            _weapon.Equipped = true;
        }
    }

    private Item _helmet = Item.NoneItem();

    [Export]
    public Item Helmet
    {
        get => _helmet;
        set
        {
            _helmet.Equipped = false;
            _helmet = value;
            _helmet.Equipped = true;
        }
    }

    public AttributeBonus ComputeAttributeBonus()
    {
        return Armor.ArmorStats.AttributeBonuses 
               + Weapon.WeaponStats.AttributeBonuses 
               + Helmet.WearableStats.AttributeBonuses;
    }

    public SkillBonus ComputeSkillBonus()
    {
        return Armor.ArmorStats.SkillBonuses
            + Weapon.WeaponStats.SkillBonuses
            + Helmet.WearableStats.SkillBonuses;
    }

    public int ComputeAc()
    {
        return Armor.ArmorStats.Ac;
    }

    public DamageRoll GetDamageRolls()
    {
        return _weapon.WeaponStats.DamageRolls;
    }

    public int ComputeToHitMod()
    {
        return _weapon.WeaponStats.ToHitMod;
    }
}