using Godot;
using System;

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
        int ret = 0;
        for (int i = 0; i < Rolls.Count; i++)
        {
            ret += random.Next(1, Rolls[i]);
        }
        ret += Mod;
        return ret;
    }
}
