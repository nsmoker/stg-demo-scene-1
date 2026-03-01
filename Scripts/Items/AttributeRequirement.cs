using Godot;

namespace ArkhamHunters.Scripts.Items;

[Tool]
[GlobalClass]
public partial class AttributeRequirement : Resource
{
    [Export]
    public int MinimumStrength;
    [Export]
    public int MinimumDexterity;
    [Export]
    public int MinimumEndurance;
    [Export]
    public int MinimumIntelligence;
    [Export]
    public int MinimumWisdom;
    [Export]
    public int MinimumCharisma;
    [Export]
    public int MinimumWillpower;

    public bool MeetsRequirements(AttributeSet attributes) => attributes.Strength >= MinimumStrength && attributes.Dexterity >= MinimumDexterity
            && attributes.Endurance >= MinimumEndurance
            && attributes.Intelligence >= MinimumIntelligence
            && attributes.Wisdom >= MinimumWisdom
            && attributes.Charisma >= MinimumCharisma
            && attributes.Willpower >= MinimumWillpower;
}
