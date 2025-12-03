using EverydayDialogueEditor;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;

[Tool]
public partial class DialogueEditor : Control
{
    private Panel _contextMenu;
    public GraphEdit EditorNode;
    private List<DialogueNode> _entryPoints = [];
    private List<DialogueNode> _selection = [];
    private ushort _entryCounter = 0;
    private ushort _nodeCounter = 0;
    private ushort _responseCounter = 0;
    private ushort _actionCounter = 0;

    private Conversation _editedConversation;
    private Label _statusLabel;

    public EditorUndoRedoManager undoRedoManager;

    public override void _Ready()
    {
        _contextMenu = GetNode<Panel>("ContextMenu");

        _statusLabel = new Label();
        _statusLabel.CustomMinimumSize = new Vector2(400, 30);
        _statusLabel.Text = "Dialogue Editor - Unsaved.";

        EditorNode = GetNode<GraphEdit>("GraphEdit");

        EditorNode.GuiInput += OnInputEvent;
        EditorNode.GetMenuHBox().AddChild(_statusLabel);

        EditorNode.NodeSelected += (Node node) =>
        {
            if (node is DialogueNode dnode)
            {
                _selection.Add(dnode);
            }
        };

        EditorNode.NodeDeselected += (Node node) =>
        {
            if (node is DialogueNode dnode)
            {
                _selection.Remove(dnode);
            }
        };

        var dialogueButton = _contextMenu.GetNode<Button>("VBoxContainer/DialogueButton");
        var entryButton = _contextMenu.GetNode<Button>("VBoxContainer/EntryButton");
        var responseButton = _contextMenu.GetNode<Button>("VBoxContainer/ResponseButton");
        var actionButton = _contextMenu.GetNode<Button>("VBoxContainer/ActionButton");
        var conditionButton = _contextMenu.GetNode<Button>("VBoxContainer/ConditionButton");
        var removeConditionButton = _contextMenu.GetNode<Button>("VBoxContainer/RemoveConditionButton");
        dialogueButton.Pressed += () =>
        {
            AddNode("res://addons/edi/Scenes/dialogue_node.tscn", GetLocalMousePosition());
        };

        entryButton.Pressed += () =>
        {
            AddNode("res://addons/edi/Scenes/entry_node.tscn", GetLocalMousePosition());
        };

        responseButton.Pressed += () =>
        {
            AddNode("res://addons/edi/Scenes/response_node.tscn", GetLocalMousePosition());
        };

        actionButton.Pressed += () =>
        {
            AddNode("res://addons/edi/Scenes/action_node.tscn", GetLocalMousePosition());
        };

        conditionButton.Pressed += () =>
        {
            _selection[0].AddCondition();
        };

        removeConditionButton.Pressed += () =>
        {
            _selection[0].RemoveCondition();
        };

        EditorNode.ConnectionRequest += OnConnectionRequest;
        EditorNode.DisconnectionRequest += OnDisconnectionRequest;
        EditorNode.DeleteNodesRequest += RemoveNodes;

        EditorNode.RemoveValidConnectionType(0, 0);
        EditorNode.RemoveValidConnectionType(1, 1);

        EditorNode.AddValidConnectionType(1, 0);
    }

    public void SetConversationResource(Conversation conversation, bool saveCurrent = true)
    {
        LoadConversation(conversation, saveCurrent);
    }

    private void LoadConversation(Conversation conversation, bool saveCurrent = true)
    {
        if (EditorNode.GetChildren().Any(x => x is DialogueNode) && saveCurrent)
        {
            Save();
        }

        foreach (var node in EditorNode.GetChildren())
        {
            if (node is DialogueNode)
            {
                node.QueueFree();
            }
        }

        _nodeCounter = 0;
        _responseCounter = 0;
        _actionCounter = 0;
        _entryCounter = 0;
        _entryPoints.Clear();

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
            var nodePath = "";
            switch (node.NodeType)
            {
                case DialogueNodeType.Node:
                    nodePath = "res://addons/edi/Scenes/dialogue_node.tscn";
                    break;
                case DialogueNodeType.PlayerResponse:
                    nodePath = "res://addons/edi/Scenes/response_node.tscn";
                    break;
                case DialogueNodeType.ScriptAction:
                    nodePath = "res://addons/edi/Scenes/action_node.tscn";
                    break;
                case DialogueNodeType.ScriptEntry:
                    nodePath = "res://addons/edi/Scenes/entry_node.tscn";
                    break;
            }
            var editorNode = _AddNodeInternal(nodePath);
            editorNode.Speaker = node.Speaker;
            editorNode.NodeType = node.NodeType;
            editorNode.Addressee = node.Addressee;
            editorNode.Content = node.Content;
            editorNode.Condition = node.Condition;
            editorNode.Action = node.Action;
            EditorNode.AddChild(editorNode);
            editorNode.PositionOffset = node.EditorPos;
            if (node.EditorSize != Vector2.Zero)
            {
                editorNode.Size = node.EditorSize;
            }
            nodes.Add(editorNode);
        }

        foreach (var conn in conversation?.Connections)
        {
            EditorNode.ConnectNode(nodes[conn.fromNode].Name, 0, nodes[conn.toNode].Name, 0);
        }

        _editedConversation = conversation;
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
        switch (newNode.NodeType)
        {
            case DialogueNodeType.Node:
                {
                    newNode.Title += $" {_nodeCounter}";
                    _nodeCounter++;
                    break;
                }
            case DialogueNodeType.PlayerResponse:
                {
                    newNode.Title += $" {_responseCounter}";
                    _responseCounter++;
                    break;
                }
            case DialogueNodeType.ScriptAction:
                {
                    newNode.Title += $" {_actionCounter}";
                    _actionCounter++;
                    break;
                }
            case DialogueNodeType.ScriptEntry:
                {
                    newNode.Title += $" {_entryCounter}";
                    _entryCounter++;
                    break;
                }
        }

        newNode.DNodeId |= _nodeCounter;
        newNode.DNodeId |= ((ulong) _actionCounter) << 16;
        newNode.DNodeId |= ((ulong) _responseCounter) << 32;
        newNode.DNodeId |= ((ulong) _entryCounter) << 48;

        if (newNode.NodeType == DialogueNodeType.ScriptEntry)
        {
            _entryPoints.Add(newNode);
        }

        return newNode;
    }

    public DialogueNode AddNode(string localPath, Vector2 pos = new Vector2() )
    {
        var newNode = _AddNodeInternal(localPath);
        newNode.PositionOffset = pos;
        undoRedoManager.CreateAction($"Add {newNode.Title}");
        undoRedoManager.AddDoMethod(this, MethodName.AddAndEnableNode, newNode);
        undoRedoManager.AddUndoMethod(this, MethodName.DisableNode, newNode);
        undoRedoManager.AddDoReference(newNode);
        undoRedoManager.CommitAction();
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

    }

    public void DisableNode(DialogueNode node)
    {
        node.Visible = false;
        node.ProcessMode = ProcessModeEnum.Disabled;
    }

    public void DisableNodes(Godot.Collections.Array<StringName> names)
    {
        foreach (var name in names)
        {
            var node = EditorNode.GetNode<GraphNode>(name.ToString());
            var dnode = (DialogueNode) node;
            if (dnode.NodeType == DialogueNodeType.ScriptEntry)
            {
                _entryPoints.Remove(dnode);
            }
            node.ProcessMode = ProcessModeEnum.Disabled;
            node.Visible = false;

            var conns = EditorNode.GetConnectionListFromNode(name);
            foreach (var conn in conns)
            {
                EditorNode.DisconnectNode((StringName) conn["from_node"], (int) conn["from_port"], (StringName) conn["to_node"], (int) conn["to_port"]);
            }
        }
    }

    public void ReenableNodes(Godot.Collections.Array<StringName> names, Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Dictionary>> connectionLists)
    {
        foreach (var name in names)
        {
            var node = EditorNode.GetNode(name.ToString());
            var dnode = (DialogueNode) node;
            if (dnode.NodeType == DialogueNodeType.ScriptEntry)
            {
                _entryPoints.Add(dnode);
            }
            node.ProcessMode = ProcessModeEnum.Inherit;
            ((GraphNode) node).Visible = true;
        }
        foreach (var list in connectionLists)
        {
            foreach (var conn in list)
            {
                EditorNode.ConnectNode((StringName) conn["from_node"], (int) conn["from_port"], (StringName) conn["to_node"], (int) conn["to_port"]);
            }
        }
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

    private void TraverseNodesDFS(DialogueNode dialogueNode,
        Godot.Collections.Array<DialogueGraphNode> ret,
        Godot.Collections.Array<DialogueConnection> retConnections,
        Dictionary<ulong, int> visitedSet
        )
    {
        var resourceForm = dialogueNode.Save();
        var loc = ret.Count;
        ret.Add(resourceForm);
        visitedSet.Add(dialogueNode.GetInstanceId(), loc);
        var connList = EditorNode.GetConnectionListFromNode(dialogueNode.Name);

        foreach (var conn in connList)
        {
            var from = EditorNode.GetNode<DialogueNode>(((StringName) conn["from_node"]).ToString());
            var child = EditorNode.GetNode<DialogueNode>(((StringName) conn["to_node"]).ToString());
            if (from.GetInstanceId() == dialogueNode.GetInstanceId() && !visitedSet.ContainsKey(child.GetInstanceId()))
            {
                retConnections.Add(new DialogueConnection
                {
                    fromNode = loc,
                    toNode = ret.Count
                });
                TraverseNodesDFS(child, ret, retConnections, visitedSet);
            }
            else if (from.GetInstanceId() == dialogueNode.GetInstanceId())
            {
                retConnections.Add(new DialogueConnection
                {
                    fromNode = loc,
                    toNode = visitedSet[child.GetInstanceId()]
                });
            }
        }
    }

    public void Save()
    {
        Godot.Collections.Array<DialogueGraphNode> entrysRet = [];
        Godot.Collections.Array<DialogueGraphNode> ret = [];
        Godot.Collections.Array<DialogueConnection> conns = [];
        
        foreach (var entryPoint in _entryPoints)
        {
            if (IsInstanceValid(entryPoint) && entryPoint.Visible)
            {
                entrysRet.Add(entryPoint.Save());

                TraverseNodesDFS(entryPoint, ret, conns, []);
            }
        }

        // Sort connections by to-node y-position.
        Godot.Collections.Array<DialogueConnection> connsRet = new(conns.OrderBy(x =>
        {
            var toNode = ret[x.toNode];
            return toNode.EditorPos.Y;
        }));

        var conv = new Conversation
        {
            Nodes = ret,
            Connections = connsRet,
            EntryPoints = entrysRet,
        };

        if (_editedConversation != null && _editedConversation.ResourcePath != null)
        {
            ResourceSaver.Save(_editedConversation);
        }
        else
        {
            var dialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                Filters = [ "*.tres;TRES Resource", "*.res;RES Resource" ],
            };
            dialog.FileSelected += path =>
            {
                conv.ResourceName = System.IO.Path.GetFileNameWithoutExtension(path);
                ResourceSaver.Save(conv, path);
                _statusLabel.Text = $"Dialogue Editor - Editing {conv.ResourcePath}";
                dialog.Hide();
                dialog.QueueFree();
            };
            dialog.Exclusive = true;
            AddChild(dialog);
            dialog.Show();
        }
    }
}