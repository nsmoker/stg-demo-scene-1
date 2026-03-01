using Godot;
using System;

[GlobalClass]
[Tool]
public partial class FactionRelationChange : DialogueAction
{
    [Export]
    private Faction Hater;

    [Export]
    private Faction Hatee;

    [Export]
    private bool Relation;

    public override void Execute(Action onComplete)
    {
        FactionSystem.SetFactionRelation(Hater, Hatee, Relation);
        onComplete?.Invoke();
    }
}
