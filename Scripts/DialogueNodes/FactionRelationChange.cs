using Godot;
using System;

[GlobalClass]
[Tool]
public partial class FactionRelationChange : DialogueAction
{
    [Export]
    Faction Hater;

    [Export]
    Faction Hatee;

    [Export]
    bool Relation;

    public override void Execute()
    {
        FactionSystem.SetFactionRelation(Hater, Hatee, Relation);
    }
}
