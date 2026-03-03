using Godot;
using STGDemoScene1.Scripts.Items;

namespace STGDemoScene1.Scripts.Resources;

[Tool]
[GlobalClass]
public partial class SkillSet : Resource
{
    [Export] public int Stealth;
    [Export] public int Mechanics;
    [Export] public int Alchemy;
    [Export] public int Rhetoric;
    [Export] public int FirstAid;
    [Export] public Godot.Collections.Array<WeaponProficiency> Proficiencies = [];
}
