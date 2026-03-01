using Godot;

namespace ArkhamHunters.Scripts.Items;

[Tool]
[GlobalClass]
public partial class ConsumableStats : Resource
{
    [Export]
    public Godot.Collections.Array<DamageType> DamageRolls;
    [Export]
    public Godot.Collections.Array<DamageType> HealRolls;
    [Export]
    public int HealMod;
    [Export]
    public int DamageMod;
    [Export]
    public SkillBonus SkillBonuses;
    [Export]
    public AttributeBonus AttributeBonuses;
}
