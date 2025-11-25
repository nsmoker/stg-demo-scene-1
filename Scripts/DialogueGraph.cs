using System.Collections.Generic;

namespace ArkhamHunters.Scripts;

public struct DialogueChoice
{
    public string Phrase;
    public DialogueGraph Continuation;
}

public class DialogueGraph
{
    public List<string> Phrases = new();
    public List<DialogueChoice> Choices = new();

    public DialogueGraph(List<string> phrases, List<DialogueChoice> choices)
    {
        Phrases.AddRange(phrases);
        Choices.AddRange(choices);
    }
}