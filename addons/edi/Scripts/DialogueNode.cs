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
    [Export]
    public DialogueCondition Condition;

    private LineEdit _speakerEdit;
    private LineEdit _addresseeEdit;
    private TextEdit _contentEdit;
    private EditorResourcePicker _editorResourcePicker;

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

        _editorResourcePicker = new EditorResourcePicker();
        _editorResourcePicker.CustomMinimumSize = new Vector2(400, 30);
        _editorResourcePicker.BaseType = "DialogueCondition";
        _editorResourcePicker.ResourceChanged += (Resource resource) =>
        {
            GD.Print("HIT");
            if (_editorResourcePicker.EditedResource != null && _editorResourcePicker.EditedResource is DialogueCondition condition)
            {
                GD.Print("HIT 2");
                Condition = condition;
            };
        };

        _editorResourcePicker.Visible = false;

        if (Condition != null)
        {
            _editorResourcePicker.EditedResource = Condition;
            _editorResourcePicker.Visible = true;
        }

        AddChild(_editorResourcePicker);
    }

    public void AddCondition()
    {
        _editorResourcePicker.Visible = true;
    }

    public void RemoveCondition()
    {
        _editorResourcePicker.Visible = false;
        _editorResourcePicker.EditedResource = null;
        Condition = null;
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
            Condition = Condition,
        };
    }
}
