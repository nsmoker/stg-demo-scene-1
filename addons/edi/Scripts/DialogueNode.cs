#if TOOLS
using Godot;

namespace STGDemoScene1.Addons.Edi.Scripts;

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
    public ulong DNodeId;
    [Export]
    public DialogueCondition Condition;
    [Export]
    public DialogueAction Action;
    [Export]
    public ulong LinkDNodeId;

    private LineEdit _speakerEdit;
    private LineEdit _addresseeEdit;
    private TextEdit _contentEdit;
    private EditorResourcePicker _editorConditionPicker;
    private EditorResourcePicker _editorActionPicker;
    private OptionButton _linkButton;

    public override void _Ready()
    {
        if (NodeType is not DialogueNodeType.ScriptAction and not DialogueNodeType.ScriptEntry)
        {
            _speakerEdit = GetNode<LineEdit>("Speaker");
            _speakerEdit.Text = Speaker;
            _addresseeEdit = GetNode<LineEdit>("Addressee");
            _addresseeEdit.Text = Addressee;
            _contentEdit = GetNode<TextEdit>("Content");
            _contentEdit.Text = Content;
        }

        _editorConditionPicker = new EditorResourcePicker
        {
            CustomMinimumSize = new Vector2(400, 30),
            BaseType = "DialogueCondition"
        };
        _editorConditionPicker.ResourceChanged += _ =>
        {
            if (_editorConditionPicker.EditedResource is DialogueCondition condition)
            {
                Condition = condition;
            }
        };
        _editorConditionPicker.ResourceSelected += OnResourceEditRequest;

        _editorConditionPicker.Visible = false;

        if (Condition != null)
        {
            _editorConditionPicker.EditedResource = Condition;
            _editorConditionPicker.Visible = true;
        }

        if (NodeType == DialogueNodeType.ScriptAction)
        {
            _editorActionPicker = new EditorResourcePicker
            {
                CustomMinimumSize = new Vector2(400, 30),
                BaseType = "DialogueAction"
            };
            _editorActionPicker.ResourceChanged += _ =>
            {
                if (_editorActionPicker.EditedResource is DialogueAction action)
                {
                    Action = action;
                }
            };
            _editorActionPicker.ResourceSelected += OnResourceEditRequest;

            if (Action != null)
            {
                _editorActionPicker.EditedResource = Action;
                _editorActionPicker.Visible = true;
            }

            AddChild(_editorActionPicker);
        }

        _linkButton = GetNode<OptionButton>("LinkOptionButton");
        _linkButton.GetPopup().MinSize = new Vector2I(_linkButton.GetPopup().MinSize.X, 300);
        _linkButton.GetPopup().MaxSize = new Vector2I(_linkButton.GetPopup().MaxSize.X, 300);

        AddChild(_editorConditionPicker);
    }

    public void AddCondition() => _editorConditionPicker.Visible = true;

    public void RemoveCondition()
    {
        _editorConditionPicker.Visible = false;
        _editorConditionPicker.EditedResource = null;
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
            EditorPos = PositionOffset,
            EditorSize = Size,
            Action = Action,
            LinkDNodeId = _linkButton.Selected >= 0 ? (ulong) _linkButton.GetItemMetadata(_linkButton.Selected) : 0
        };
    }

    public void SetLinkOptions(System.Collections.Generic.List<DialogueNode> nodes)
    {

        var selection = _linkButton.Selected;
        var selectionId = selection >= 0 ? (ulong) _linkButton.GetItemMetadata(_linkButton.Selected) : 0;
        _linkButton.Clear();
        foreach (var node in nodes)
        {
            _linkButton.AddItem($"{node.Title}");
            _linkButton.SetItemMetadata(_linkButton.GetItemCount() - 1, node.DNodeId);
        }

        if (selection >= 0)
        {
            var currentSelectionIndex = nodes.FindIndex(node => node.DNodeId == selectionId);
            _linkButton.Selected = currentSelectionIndex;
        }
        else if (LinkDNodeId != 0)
        {
            var linkIndex = nodes.FindIndex(node => node.DNodeId == LinkDNodeId);
            _linkButton.Selected = linkIndex;
            LinkDNodeId = linkIndex >= 0 ? nodes[linkIndex].DNodeId : 0;
        }
        else
        {
            _linkButton.Selected = -1;
        }
    }

    private static void OnResourceEditRequest(Resource resource, bool _) => EditorInterface.Singleton.GetInspector().Edit(resource);
}
#endif
