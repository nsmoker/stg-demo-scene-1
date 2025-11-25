using Godot;

namespace ArkhamHunters.Scripts.Items;

[GlobalClass]
public partial class WeaponStats : Resource
{
    [Export] public DamageRoll DamageRolls = new();
    [Export] public int ToHitMod;
    [Export] public SkillBonus SkillBonuses = new();
    [Export] public AttributeBonus AttributeBonuses = new();
    [Export] public bool Melee;
    [Export] public float Range;
}