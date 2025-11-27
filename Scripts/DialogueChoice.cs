using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkhamHunters.Scripts;
[GlobalClass]
public partial class DialogueChoice : Resource
{
    [Export]
    public string Phrase;
    [Export]
    public DialogueGraph Continuation;

    public DialogueChoice()
    {
        Phrase = "";
        Continuation = new DialogueGraph();
    }
}
