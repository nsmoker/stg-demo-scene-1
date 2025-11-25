using Godot;

namespace ArkhamHunters.Scripts.Items;

[GlobalClass]
public partial class SkillBonus: Resource
{
    [Export]
    public int AlchemyBonus;
    [Export]
    public int StealthBonus;
    [Export]
    public int RhetoricBonus;
    [Export]
    public int MechanicsBonus;
    [Export]
    public int FirstAidBonus;
    [Export]
    public Godot.Collections.Array<WeaponProficiency> BonusProficiencies;

    public static SkillBonus operator +(SkillBonus m1, SkillBonus m2)
    {
        var ret = new SkillBonus();
        ret.AlchemyBonus = m1.AlchemyBonus + m2.AlchemyBonus;
        ret.StealthBonus = m1.StealthBonus + m2.StealthBonus;
        ret.RhetoricBonus = m1.RhetoricBonus + m2.RhetoricBonus;
        ret.MechanicsBonus = m1.MechanicsBonus + m2.MechanicsBonus;
        ret.FirstAidBonus = m1.FirstAidBonus + m2.FirstAidBonus;
        return ret;
    }

    public bool IsSignificant()
    {
        return AlchemyBonus != 0 || StealthBonus != 0 || RhetoricBonus != 0 || MechanicsBonus != 0 || FirstAidBonus != 0;
    }
}