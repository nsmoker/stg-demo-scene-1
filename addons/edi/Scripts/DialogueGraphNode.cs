using Godot;

namespace STGDemoScene1.Addons.Edi.Scripts;

public enum DialogueNodeType
{
    Node,
    ScriptAction,
    ScriptEntry,
    PlayerResponse,
}

[Tool]
[GlobalClass]
public partial class DialogueGraphNode : Resource
{
    [Export]
    public DialogueNodeType NodeType;
    [Export]
    public string Content;
    [Export]
    public string Speaker;
    [Export]
    public string Addressee;
    [Export]
    public ulong DNodeId = 0;
    [Export]
    public DialogueCondition Condition;
    [Export]
    public DialogueAction Action;

    [Export]
    public ulong LinkDNodeId = 0;

    [Export]
    public Vector2 EditorPos = Vector2.Zero;
    [Export]
    public Vector2 EditorSize = Vector2.Zero;

    public DialogueGraphNode() { }
}
