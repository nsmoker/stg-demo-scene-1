using Godot;

namespace STGDemoScene1.Scripts.Items;

[Tool]
[GlobalClass]
public partial class AttributeBonus : Resource
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
        var ret = new AttributeBonus
        {
            StrengthBonus = a.StrengthBonus + b.StrengthBonus,
            DexterityBonus = a.DexterityBonus + b.DexterityBonus,
            EnduranceBonus = a.EnduranceBonus + b.EnduranceBonus,
            IntelligenceBonus = a.IntelligenceBonus + b.IntelligenceBonus,
            WisdomBonus = a.WisdomBonus + b.WisdomBonus,
            CharismaBonus = a.CharismaBonus + b.CharismaBonus,
            WillpowerBonus = a.WillpowerBonus + b.WillpowerBonus
        };
        return ret;
    }

    public bool IsSignificant() => StrengthBonus != 0
               || DexterityBonus != 0
               || EnduranceBonus != 0
               || IntelligenceBonus != 0
               || WisdomBonus != 0
               || CharismaBonus != 0
               || WillpowerBonus != 0;
}
