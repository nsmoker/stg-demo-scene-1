using EverydayDialogueEditor;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class DialogueEditor : Control
{
    private Panel _contextMenu;
    private GraphEdit _editor;
    private List<DialogueNode> _entryPoints = [];
    private List<DialogueNode> _selection = [];
    private ushort _entryCounter = 0;
    private ushort _nodeCounter = 0;
    private ushort _responseCounter = 0;
    private ushort _actionCounter = 0;

    private EditorResourcePicker _editorResourcePicker;

    public EditorUndoRedoManager undoRedoManager;

    public override void _Ready()
    {
        _contextMenu = GetNode<Panel>("ContextMenu");

        _editorResourcePicker = new EditorResourcePicker();
        _editorResourcePicker.CustomMinimumSize = new Vector2(400, 30);
        _editorResourcePicker.BaseType = "Conversation";
        _editorResourcePicker.ResourceChanged += (Resource resource) =>
        {
            if (_editorResourcePicker.EditedResource != null && _editorResourcePicker.EditedResource is Conversation conversation)
            {
                LoadConversation(conversation);
            };
        };

        if (_editorResourcePicker.EditedResource != null && _editorResourcePicker.EditedResource is Conversation conversation)
        {
            LoadConversation(conversation);
        }

        _editor = GetNode<GraphEdit>("GraphEdit");

        _editor.GuiInput += OnInputEvent;
        _editor.GetMenuHBox().AddChild(_editorResourcePicker);

        _editor.NodeSelected += (Node node) =>
        {
            if (node is DialogueNode dnode)
            {
                _selection.Add(dnode);
            }
        };

        _editor.NodeDeselected += (Node node) =>
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

        _editor.ConnectionRequest += OnConnectionRequest;
        _editor.DisconnectionRequest += OnDisconnectionRequest;
        _editor.DeleteNodesRequest += RemoveNodes;

        _editor.RemoveValidConnectionType(0, 0);
        _editor.RemoveValidConnectionType(1, 1);

        _editor.AddValidConnectionType(1, 0);
    }

    public void LoadConversation(Conversation conversation)
    {
        if (conversation != null)
        {
            foreach (var node in _editor.GetChildren())
            {
                if (node is GraphNode)
                {
                    node.QueueFree();
                }
            }

            List<DialogueNode> nodes = new();
            foreach (var node in conversation.Nodes)
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
                _editor.AddChild(editorNode);
                editorNode.PositionOffset = node.EditorPos;
                if (node.EditorSize != Vector2.Zero)
                {
                    editorNode.Size = node.EditorSize;
                }
                nodes.Add(editorNode);
            }

            foreach (var conn in conversation.Connections)
            {
                _editor.ConnectNode(nodes[conn.fromNode].Name, 0, nodes[conn.toNode].Name, 0);
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
            _editor.AddChild(node);
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
            var node = _editor.GetNode<GraphNode>(name.ToString());
            var dnode = (DialogueNode) node;
            if (dnode.NodeType == DialogueNodeType.ScriptEntry)
            {
                _entryPoints.Remove(dnode);
            }
            node.ProcessMode = ProcessModeEnum.Disabled;
            node.Visible = false;

            var conns = _editor.GetConnectionListFromNode(name);
            foreach (var conn in conns)
            {
                _editor.DisconnectNode((StringName) conn["from_node"], (int) conn["from_port"], (StringName) conn["to_node"], (int) conn["to_port"]);
            }
        }
    }

    public void ReenableNodes(Godot.Collections.Array<StringName> names, Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Dictionary>> connectionLists)
    {
        foreach (var name in names)
        {
            var node = _editor.GetNode(name.ToString());
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
                _editor.ConnectNode((StringName) conn["from_node"], (int) conn["from_port"], (StringName) conn["to_node"], (int) conn["to_port"]);
            }
        }
    }

    public void RemoveNodes(Godot.Collections.Array<StringName> names)
    {
        Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Dictionary>> connectionLists = [];
        foreach (var name in names)
        {
            connectionLists.Add(_editor.GetConnectionListFromNode(name));
        }
        undoRedoManager.CreateAction($"Remove {names.Count} nodes");
        undoRedoManager.AddDoMethod(this, MethodName.DisableNodes, names);
        undoRedoManager.AddUndoMethod(this, MethodName.ReenableNodes, names, connectionLists);
        foreach (var name in names)
        {
            undoRedoManager.AddUndoReference(_editor.GetNode(name.ToString()));
        }
        undoRedoManager.CommitAction();
    }

    public void OnConnectionRequest(StringName from, long fromPort, StringName to, long toPort)
    {
        undoRedoManager.CreateAction($"Connect {from} to {to}");
        undoRedoManager.AddDoMethod(_editor, GraphEdit.MethodName.ConnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.AddUndoMethod(_editor, GraphEdit.MethodName.DisconnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.CommitAction();
    }

    public void OnDisconnectionRequest(StringName from, long fromPort, StringName to, long toPort)
    {
        undoRedoManager.CreateAction($"Disconnect {from} from {to}");
        undoRedoManager.AddDoMethod(_editor, GraphEdit.MethodName.DisconnectNode, from, (int) fromPort, to, (int) toPort);
        undoRedoManager.AddUndoMethod(_editor, GraphEdit.MethodName.ConnectNode, from, (int) fromPort, to, (int) toPort);
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
        var connList = _editor.GetConnectionListFromNode(dialogueNode.Name);

        foreach (var conn in connList)
        {
            var from = _editor.GetNode<DialogueNode>(((StringName) conn["from_node"]).ToString());
            var child = _editor.GetNode<DialogueNode>(((StringName) conn["to_node"]).ToString());
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
        Godot.Collections.Array<DialogueConnection> connsRet = [];
        
        foreach (var entryPoint in _entryPoints)
        {
            if (IsInstanceValid(entryPoint) && entryPoint.Visible)
            {
                entrysRet.Add(entryPoint.Save());

                TraverseNodesDFS(entryPoint, ret, connsRet, []);
            }
        }

        var conv = new Conversation
        {
            Nodes = ret,
            Connections = connsRet,
            EntryPoints = entrysRet,
        };

        if (_editorResourcePicker.EditedResource != null)
        {
            var edit = (Conversation) _editorResourcePicker.EditedResource;
            edit.Nodes = conv.Nodes;
            edit.Connections = conv.Connections;
            edit.EntryPoints = conv.EntryPoints;

            ResourceSaver.Save(edit);
        }
    }
}