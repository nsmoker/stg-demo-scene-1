using Godot;

namespace STGDemoScene1.Scripts.Items;

[Tool]
[GlobalClass]
public partial class WearableStats : Resource
{
    [Export] public SkillBonus SkillBonuses = new();
    [Export] public AttributeBonus AttributeBonuses = new();
    [Export] public Godot.Collections.Array<WeaponProficiency> BonusProficiencies = [];
}
