
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Addons.Edi.Scripts;

[Tool]
public partial class DialogueEditor : Control
{
    private Panel _contextMenu;
    public GraphEdit EditorNode;
    private readonly List<DialogueNode> _selection = [];
    private readonly List<DialogueNode> _clipboard = [];
    private ulong _nodeCounter = 0;
    private Conversation _editedConversation;
    private Label _statusLabel;

    public EditorUndoRedoManager undoRedoManager;

    public override void _Ready()
    {
        _contextMenu = GetNode<Panel>("ContextMenu");

        _statusLabel = new Label
        {
            CustomMinimumSize = new Vector2(400, 30),
            Text = "Dialogue Editor - Unsaved."
        };

        EditorNode = GetNode<GraphEdit>("GraphEdit");

        EditorNode.GuiInput += OnInputEvent;
        EditorNode.GetMenuHBox().AddChild(_statusLabel);
        EditorNode.GetMenuHBox().AddChild(CreateRegenIdsButton());

        EditorNode.NodeSelected += node =>
        {
            if (node is DialogueNode dnode)
            {
                _selection.Add(dnode);
            }
        };

        EditorNode.NodeDeselected += node =>
        {
            if (node is DialogueNode dnode)
            {
                _ = _selection.Remove(dnode);
            }
        };

        var dialogueButton = _contextMenu.GetNode<Button>("VBoxContainer/DialogueButton");
        var entryButton = _contextMenu.GetNode<Button>("VBoxContainer/EntryButton");
        var responseButton = _contextMenu.GetNode<Button>("VBoxContainer/ResponseButton");
        var actionButton = _contextMenu.GetNode<Button>("VBoxContainer/ActionButton");
        var conditionButton = _contextMenu.GetNode<Button>("VBoxContainer/ConditionButton");
        var removeConditionButton = _contextMenu.GetNode<Button>("VBoxContainer/RemoveConditionButton");
        dialogueButton.Pressed += () => _ = AddNode("res://addons/edi/Scenes/dialogue_node.tscn", GetLocalMousePosition());

        entryButton.Pressed += () => _ = AddNode("res://addons/edi/Scenes/entry_node.tscn", GetLocalMousePosition());

        responseButton.Pressed += () => _ = AddNode("res://addons/edi/Scenes/response_node.tscn", GetLocalMousePosition());

        actionButton.Pressed += () => AddNode("res://addons/edi/Scenes/action_node.tscn", GetLocalMousePosition());

        conditionButton.Pressed += () => _selection[0].AddCondition();

        removeConditionButton.Pressed += () => _selection[0].RemoveCondition();

        EditorNode.ConnectionRequest += OnConnectionRequest;
        EditorNode.DisconnectionRequest += OnDisconnectionRequest;
        EditorNode.DeleteNodesRequest += RemoveNodes;

        EditorNode.RemoveValidConnectionType(0, 0);
        EditorNode.RemoveValidConnectionType(1, 1);

        EditorNode.AddValidConnectionType(1, 0);

        EditorNode.CopyNodesRequest += OnCopyRequest;
        EditorNode.CutNodesRequest += OnCutRequest;
        EditorNode.PasteNodesRequest += OnPasteRequest;
    }

    public void SetConversationResource(Conversation conversation, bool saveCurrent = true) => LoadConversation(conversation, saveCurrent);

    private void LoadConversation(Conversation conversation, bool saveCurrent = true)
    {
        if (EditorNode.GetChildren().Any(x => x is DialogueNode) && saveCurrent)
        {
            Save();
        }

        EditorNode.ClearConnections();

        foreach (var node in EditorNode.GetChildren())
        {
            if (node is DialogueNode)
            {
                node.Free();
            }
        }

        _nodeCounter = 0;

        if (conversation == null)
        {
            _statusLabel.Text = "Dialogue Editor - No conversation loaded.";
            _editedConversation = null;
            return;
        }
        _statusLabel.Text = $"Dialogue Editor - Editing {conversation.ResourcePath}";

        List<DialogueNode> nodes = [];
        foreach (var node in conversation?.Nodes)
        {
            var nodePath = node.NodeType switch
            {
                DialogueNodeType.Node => "res://addons/edi/Scenes/dialogue_node.tscn",
                DialogueNodeType.PlayerResponse => "res://addons/edi/Scenes/response_node.tscn",
                DialogueNodeType.ScriptAction => "res://addons/edi/Scenes/action_node.tscn",
                DialogueNodeType.ScriptEntry => "res://addons/edi/Scenes/entry_node.tscn",
                _ => "",
            };
            var editorNode = _AddNodeInternal(nodePath);
            editorNode.Speaker = node.Speaker;
            editorNode.NodeType = node.NodeType;
            editorNode.Addressee = node.Addressee;
            editorNode.Content = node.Content;
            editorNode.Condition = node.Condition;
            editorNode.Action = node.Action;
            EditorNode.AddChild(editorNode);
            editorNode.PositionOffset = node.EditorPos;
            editorNode.LinkDNodeId = node.LinkDNodeId;
            editorNode.DNodeId = node.DNodeId;
            if (node.EditorSize != Vector2.Zero)
            {
                editorNode.Size = node.EditorSize;
            }
            nodes.Add(editorNode);
        }

        foreach (var conn in conversation?.Connections)
        {
            _ = EditorNode.ConnectNode(nodes[conn.fromNode].Name, 0, nodes[conn.toNode].Name, 0);
        }

        _editedConversation = conversation;

        UpdateLinkOptions();
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        // This isn't documented anywhere by Godot, so could break at any time.
        if (data.VariantType == Variant.Type.Dictionary)
        {
            var dict = data.AsGodotDictionary();
            switch (dict["type"].AsString())
            {
                case "files":
                    {
                        var path = dict["files"].AsGodotArray()[0].AsString();
                        var res = ResourceLoader.Load(path);
                        return res is Conversation;

                    }
                case "obj_property":
                    {
                        var val = dict["value"];
                        return val.VariantType == Variant.Type.Object && val.AsGodotObject() is Conversation;
                    }
                case "resource":
                    {
                        var val = dict["resource"].AsGodotObject();
                        return val is Conversation;
                    }
                default:
                    return false;
            }
        }
        else
        {
            return false;
        }
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        // This isn't documented anywhere by Godot, so could break at any time.
        if (data.VariantType == Variant.Type.Dictionary)
        {
            var dict = data.AsGodotDictionary();
            switch (dict["type"].AsString())
            {
                case "files":
                    {
                        var path = dict["files"].AsGodotArray()[0].AsString();
                        var res = ResourceLoader.Load(path);
                        if (res is Conversation conversation)
                        {
                            LoadConversation(conversation);
                        }
                        break;
                    }
                case "obj_property":
                    {
                        var val = dict["value"];
                        if (val.VariantType == Variant.Type.Object && val.AsGodotObject() is Conversation conversation)
                        {
                            LoadConversation(conversation);
                        }
                        break;
                    }
                case "resource":
                    {
                        var val = dict["resource"].AsGodotObject();
                        if (val is Conversation conversation)
                        {
                            LoadConversation(conversation);
                        }
                        break;
                    }
                default:
                    break;
            }
        }
    }

    public void OnInputEvent(InputEvent e)
    {
        if (e is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.IsPressed())
            {
                _contextMenu.Visible = !_contextMenu.Visible;
                _contextMenu.Position = GetLocalMousePosition();

                _contextMenu.GetNode<Button>("VBoxContainer/ConditionButton").Visible = _selection.Count == 1 && _selection.Where(x => x.NodeType != DialogueNodeType.ScriptEntry).Count() == 1;
                _contextMenu.GetNode<Button>("VBoxContainer/RemoveConditionButton").Visible =
                    _selection.Count == 1 && _selection.Where(x => x.NodeType != DialogueNodeType.ScriptEntry && x.Condition != null).Count() == 1;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
            {
                _contextMenu.Visible = false;
            }
        }
    }

    private DialogueNode _AddNodeInternal(string localPath)
    {
        var newNode = GD.Load<PackedScene>(ProjectSettings.GlobalizePath(localPath)).Instantiate<DialogueNode>();

        newNode.Title += $" {_nodeCounter}";
        newNode.DNodeId = _nodeCounter;
        _nodeCounter++;

        return newNode;
    }

    public DialogueNode AddNode(string localPath, Vector2 _ = new Vector2())
    {
        var newNode = _AddNodeInternal(localPath);
        newNode.PositionOffset = (EditorNode.ScrollOffset + EditorNode.Size / 2) / EditorNode.Zoom - newNode.Size / 2;
        undoRedoManager.CreateAction($"Add {newNode.Title}");
        undoRedoManager.AddDoMethod(this, MethodName.AddAndEnableNode, newNode);
        undoRedoManager.AddUndoMethod(this, MethodName.DisableNode, newNode);
        undoRedoManager.AddDoReference(newNode);
        undoRedoManager.CommitAction();

        UpdateLinkOptions(); // Refresh options after adding a node
        return newNode;
    }

    public void AddAndEnableNode(DialogueNode node)
    {
        if (node.GetParent() == null)
        {
            EditorNode.AddChild(node);
        }

        node.Visible = true;
        node.ProcessMode = ProcessModeEnum.Always;
        UpdateLinkOptions(); // Refresh options after adding and enabling a node
    }

    public void DisableNode(DialogueNode node)
    {
        node.Visible = false;
        node.ProcessMode = ProcessModeEnum.Disabled;
        UpdateLinkOptions(); // Refresh options after disabling a node
    }

    public void DisableNodes(Godot.Collections.Array<StringName> names)
    {
        foreach (var name in names)
        {
            var node = EditorNode.GetNode<GraphNode>(name.ToString());
            var dnode = (DialogueNode) node;
            node.ProcessMode = ProcessModeEnum.Disabled;
            node.Visible = false;

            var conns = EditorNode.GetConnectionListFromNode(name);
            foreach (var conn in conns)
            {
                EditorNode.DisconnectNode((StringName) conn["from_node"], (int) conn["from_port"], (StringName) conn["to_node"], (int) conn["to_port"]);
            }
        }

        UpdateLinkOptions(); // Refresh options after disabling nodes
    }

    public void ReenableNodes(Godot.Collections.Array<StringName> names, Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Dictionary>> connectionLists)
    {
        foreach (var name in names)
        {
            var node = EditorNode.GetNode(name.ToString());
            node.ProcessMode = ProcessModeEnum.Inherit;
            ((GraphNode) node).Visible = true;
        }
        foreach (var list in connectionLists)
        {
            foreach (var conn in list)
            {
                _ = EditorNode.ConnectNode((StringName) conn["from_node"], (int) conn["from_port"], (StringName) conn["to_node"], (int) conn["to_port"]);
            }
        }

        UpdateLinkOptions(); // Refresh options after reenabling nodes
    }

    public void RemoveNodes(Godot.Collections.Array<StringName> names)
    {
        Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Dictionary>> connectionLists = [];
        foreach (var name in names)
        {
            connectionLists.Add(EditorNode.GetConnectionListFromNode(name));
        }
        undoRedoManager.CreateAction($"Remove {names.Count} nodes");
        undoRedoManager.AddDoMethod(this, MethodName.DisableNodes, names);
        undoRedoManager.AddUndoMethod(this, MethodName.ReenableNodes, names, connectionLists);
        foreach (var name in names)
        {
            undoRedoManager.AddUndoReference(EditorNode.GetNode(name.ToString()));
        }
        undoRedoManager.CommitAction();

        UpdateLinkOptions(); // Refresh options after removing nodes
    }

    public void OnConnectionRequest(StringName from, long fromPort, StringName to, long toPort)
    {
        undoRedoManager.CreateAction($"Connect {from} to {to}");
        undoRedoManager.AddDoMethod(EditorNode, GraphEdit.MethodName.ConnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.AddUndoMethod(EditorNode, GraphEdit.MethodName.DisconnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.CommitAction();
    }

    public void OnDisconnectionRequest(StringName from, long fromPort, StringName to, long toPort)
    {
        undoRedoManager.CreateAction($"Disconnect {from} from {to}");
        undoRedoManager.AddDoMethod(EditorNode, GraphEdit.MethodName.DisconnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.AddUndoMethod(EditorNode, GraphEdit.MethodName.ConnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.CommitAction();
    }

    private void OnCopyRequest()
    {
        _clipboard.Clear();
        foreach (var node in _selection)
        {
            if (node.Duplicate() is DialogueNode copy)
            {
                _clipboard.Add(copy);
            }
        }
    }

    private void OnCutRequest()
    {
        OnCopyRequest(); // Copy the selected nodes first
        RemoveNodes([.. _selection.Select(n => n.Name)]);
    }

    private void OnPasteRequest()
    {
        if (_clipboard.Count == 0)
        {
            return;
        }

        undoRedoManager.CreateAction("Paste Nodes");
        foreach (var node in _clipboard)
        {
            if (node.Duplicate() is DialogueNode pastedNode)
            {
                pastedNode.Title = pastedNode.NodeType switch
                {
                    DialogueNodeType.PlayerResponse => "Player Response",
                    DialogueNodeType.ScriptAction => "Script Action",
                    DialogueNodeType.ScriptEntry => "Script Entry",
                    _ => "Dialogue Node",
                };
                pastedNode.Title += $" {_nodeCounter}";
                pastedNode.Name = pastedNode.Title;
                pastedNode.DNodeId = _nodeCounter;
                _nodeCounter++;
                pastedNode.PositionOffset += new Vector2(50, 50); // Offset to avoid overlap
                undoRedoManager.AddDoMethod(this, MethodName.AddAndEnableNode, pastedNode);
                undoRedoManager.AddUndoMethod(this, MethodName.DisableNode, pastedNode);
            }
        }
        undoRedoManager.CommitAction();
    }

    private void UpdateLinkOptions()
    {
        var nodes = EditorNode.GetChildren().Where(x => x is DialogueNode).Select(x => (DialogueNode) x).ToList();
        foreach (var node in nodes)
        {
            if (node.Visible && !node.IsQueuedForDeletion())
            {
                node.SetLinkOptions(nodes);
            }
        }
    }

    public void Save()
    {
        List<DialogueGraphNode> ret = [];
        HashSet<DialogueConnection> conns = [];
        List<DialogueGraphNode> entrysRet = [];

        foreach (var child in EditorNode.GetChildren())
        {
            if (child is DialogueNode dialogueNode && dialogueNode.Visible)
            {
                ret.Add(dialogueNode.Save());
                if (dialogueNode.NodeType == DialogueNodeType.ScriptEntry)
                {
                    entrysRet.Add(dialogueNode.Save());
                }
            }
        }

        foreach (var dialogueGraphNode in ret)
        {
            var dialogueNode = EditorNode.GetChildren().First(x => x is DialogueNode n && n.DNodeId == dialogueGraphNode.DNodeId) as DialogueNode;
            var connList = EditorNode.GetConnectionListFromNode(dialogueNode.Name);
            foreach (var conn in connList)
            {
                var from = EditorNode.GetNode<DialogueNode>(((StringName) conn["from_node"]).ToString());
                var child = EditorNode.GetNode<DialogueNode>(((StringName) conn["to_node"]).ToString());

                var fromLoc = ret.IndexOf(dialogueGraphNode);
                var toLoc = ret.FindIndex(x => x.DNodeId == child.DNodeId);
                if (dialogueGraphNode.DNodeId == from.DNodeId)
                {
                    _ = conns.Add(new DialogueConnection
                    {
                        fromNode = fromLoc,
                        toNode = toLoc
                    });
                }
            }
        }

        // Sort connections by to-node y-position.
        Godot.Collections.Array<DialogueConnection> connsRet = [.. conns.OrderBy(x =>
        {
            var toNode = ret[x.toNode];
            return toNode.EditorPos.Y;
        })];

        var conv = new Conversation
        {
            Nodes = [.. ret],
            Connections = connsRet,
            EntryPoints = [.. entrysRet],
        };

        if (_editedConversation != null && _editedConversation.ResourcePath != null)
        {
            _editedConversation.Nodes = conv.Nodes;
            _editedConversation.Connections = conv.Connections;
            _editedConversation.EntryPoints = conv.EntryPoints;
            _ = ResourceSaver.Save(_editedConversation);
        }
        else
        {
            var dialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                Filters = ["*.tres;TRES Resource", "*.res;RES Resource"],
            };
            dialog.FileSelected += path =>
            {
                conv.ResourceName = System.IO.Path.GetFileNameWithoutExtension(path);
                _ = ResourceSaver.Save(conv, path);
                _statusLabel.Text = $"Dialogue Editor - Editing {conv.ResourcePath}";
                dialog.Hide();
                dialog.QueueFree();
            };
            dialog.Exclusive = true;
            AddChild(dialog);
            dialog.Show();
        }
    }

    private Button CreateRegenIdsButton()
    {
        var button = new Button
        {
            Text = "Regenerate Node IDs"
        };
        button.Pressed += RegenerateNodeIds;
        return button;
    }

    public void RegenerateNodeIds()
    {
        _nodeCounter = 0;
        foreach (var child in EditorNode.GetChildren())
        {
            if (child is DialogueNode dialogueNode)
            {
                _nodeCounter++;
                dialogueNode.DNodeId = _nodeCounter;
            }
        }
    }
}

