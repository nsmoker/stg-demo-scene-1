using Godot;
using STGDemoScene1.Scripts.Items;

namespace STGDemoScene1.Scripts.Resources;

[GlobalClass]
[Tool]
public partial class EquipmentSet : Resource
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

    public AttributeBonus ComputeAttributeBonus() => Armor.ArmorStats.AttributeBonuses
               + Weapon.WeaponStats.AttributeBonuses
               + Helmet.WearableStats.AttributeBonuses;

    public SkillBonus ComputeSkillBonus() => Armor.ArmorStats.SkillBonuses
            + Weapon.WeaponStats.SkillBonuses
            + Helmet.WearableStats.SkillBonuses;

    public int ComputeAc() => 9 + Armor.ArmorStats.Ac;

    public DamageRoll GetDamageRolls() => _weapon.WeaponStats.DamageRolls;

    public int ComputeToHitMod() => _weapon.WeaponStats.ToHitMod;
}
