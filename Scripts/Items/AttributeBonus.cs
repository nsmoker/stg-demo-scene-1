using Godot;

namespace ArkhamHunters.Scripts.Items;

[GlobalClass]
public partial class AttributeBonus: Resource
{
    [Export]
    public int StrengthBonus;
    [Export]
    public int DexterityBonus;
    [Export]
    public int EnduranceBonus;
    [Export]
    public int IntelligenceBonus;
    [Export]
    public int WisdomBonus;
    [Export]
    public int CharismaBonus;
    [Export]
    public int WillpowerBonus;

    public static AttributeBonus operator +(AttributeBonus a, AttributeBonus b)
    {
        var ret = new AttributeBonus();
        ret.StrengthBonus = a.StrengthBonus + b.StrengthBonus;
        ret.DexterityBonus = a.DexterityBonus + b.DexterityBonus;
        ret.EnduranceBonus = a.EnduranceBonus + b.EnduranceBonus;
        ret.IntelligenceBonus = a.IntelligenceBonus + b.IntelligenceBonus;
        ret.WisdomBonus = a.WisdomBonus + b.WisdomBonus;
        ret.CharismaBonus = a.CharismaBonus + b.CharismaBonus;
        ret.WillpowerBonus = a.WillpowerBonus + b.WillpowerBonus;
        return ret;
    }

    public bool IsSignificant()
    {
        return StrengthBonus != 0 
               || DexterityBonus != 0 
               || EnduranceBonus != 0 
               || IntelligenceBonus != 0 
               || WisdomBonus != 0
               || CharismaBonus != 0
               || WillpowerBonus != 0;
    }
}