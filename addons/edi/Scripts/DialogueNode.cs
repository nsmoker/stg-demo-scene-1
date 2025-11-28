using Godot;
using System;

namespace EverydayDialogueEditor;

public enum DialogueNodeType
{
    Node,
    ScriptAction,
    ScriptEntry,
    PlayerResponse,
}

[GlobalClass]
[Tool]
public partial class DialogueNode : GraphNode
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

    private LineEdit _speakerEdit;
    private LineEdit _addresseeEdit;
    private TextEdit _contentEdit;

    public override void _Ready()
    {
        if (NodeType != DialogueNodeType.ScriptAction && NodeType != DialogueNodeType.ScriptEntry)
        {
            _speakerEdit = GetNode<LineEdit>("Speaker");
            _speakerEdit.Text = Speaker;
            _addresseeEdit = GetNode<LineEdit>("Addressee");
            _addresseeEdit.Text = Addressee;
            _contentEdit = GetNode<TextEdit>("Content");
            _contentEdit.Text = Content;
        }
    }

    public DialogueGraphNode Save()
    {
        if (_speakerEdit != null)
        {
            Speaker = _speakerEdit.Text;
        }

        if (_addresseeEdit != null)
        {
            Addressee = _addresseeEdit.Text;
        }

        if (_contentEdit != null)
        {
            Content = _contentEdit.Text;
        }

        return new DialogueGraphNode
        {
            NodeType = NodeType,
            Content = Content,
            Speaker = Speaker,
            Addressee = Addressee,
            DNodeId = DNodeId,
        };
    }
}
