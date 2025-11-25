using Godot;

namespace ArkhamHunters.Scripts.Items;

[GlobalClass]
public partial class SkillRequirement: Resource
{
    [Export] public int MinimumAlchemy;
    [Export] public int MinimumStealth;
    [Export] public int MinimumRhetoric;
    [Export] public int MinimumMechanics;
    [Export] public int MinimumFirstAid;
    [Export] public Godot.Collections.Array<WeaponProficiency> Proficiencies = new();
}