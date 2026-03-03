using Godot;

namespace STGDemoScene1.Scripts.Resources;

[Tool]
public partial class AttributeSet : Resource
{
    [Export] public int Strength;
    [Export] public int Dexterity;
    [Export] public int Intelligence;
    [Export] public int Wisdom;
    [Export] public int Charisma;
    [Export] public int Endurance;
    [Export] public int Willpower;
}
