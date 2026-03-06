using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Resources.Factions;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class SetFaction : DialogueAction
{
    [Export]
    public CharacterData Character;

    [Export]
    public Faction Faction;

    public override void Execute(Action onComplete)
    {
        FactionSystem.SetFaction(Character, Faction);
        onComplete();
    }
}
