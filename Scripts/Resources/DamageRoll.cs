using Godot;
using System;
using System.Linq;

namespace STGDemoScene1.Scripts.Resources;

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

    public int Roll()
    {
        var random = new Random();
        int ret = Rolls.Sum(t => random.Next(1, t));
        ret += Mod;
        return ret;
    }
}
