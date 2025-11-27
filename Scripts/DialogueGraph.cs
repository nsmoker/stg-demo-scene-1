using Godot;
using System.Collections.Generic;

namespace ArkhamHunters.Scripts;

[GlobalClass]
public partial class DialogueGraph : Resource
{
    [Export]
    public Godot.Collections.Array<string> Phrases = new();
    [Export]
    public Godot.Collections.Array<DialogueChoice> Choices = new();

    // Do not remove.
    public DialogueGraph() { }

    public DialogueGraph(List<string> phrases, List<DialogueChoice> choices)
    {
        Phrases.AddRange(phrases);
        Choices.AddRange(choices);
    }
}