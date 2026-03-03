using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class FactionRelationChange : DialogueAction
{
    [Export]
    private Faction _hater;

    [Export]
    private Faction _hatee;

    [Export]
    private bool _relation;

    public override void Execute(Action onComplete)
    {
        FactionSystem.SetFactionRelation(_hater, _hatee, _relation);
        onComplete?.Invoke();
    }
}
