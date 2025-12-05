using Godot;
using System;

[GlobalClass]
public partial class FactionTable : Resource
{
    [Export]
    public Godot.Collections.Dictionary<Faction, Godot.Collections.Dictionary<Faction, bool>> Factions = [];

    public FactionTable() { }
}
