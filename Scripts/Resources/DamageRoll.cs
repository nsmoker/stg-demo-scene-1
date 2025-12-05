using System.Collections.Generic;
using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts;

[Tool]
[GlobalClass]
public partial class DamageRoll : Resource
{
    [Export]
    public Godot.Collections.Array<int> Rolls = [];
    [Export]
    public int Mod;
    [Export]
    public bool Melee;
}