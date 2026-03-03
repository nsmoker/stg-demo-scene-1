using Godot;

namespace STGDemoScene1.Scripts.Items;

[Tool]
[GlobalClass]
public partial class ArmorStats : Resource
{
    [Export]
    public int Ac;
    [Export]
    public SkillBonus SkillBonuses = new();
    [Export]
    public AttributeBonus AttributeBonuses = new();
}
