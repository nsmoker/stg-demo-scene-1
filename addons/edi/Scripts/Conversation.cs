using Godot;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class Conversation : Resource
{
    [Export]
    public Godot.Collections.Array<DialogueGraphNode> Nodes = [];

    [Export]
    public Godot.Collections.Array<DialogueGraphNode> EntryPoints = [];

    [Export]
    public Godot.Collections.Array<DialogueConnection> Connections = [];

    public Conversation() { }

    public List<DialogueGraphNode> GetNodeConnections(int i)
    {
        List<DialogueGraphNode> ret = [];
        foreach (var connection in Connections)
        {
            if (connection.fromNode == i)
            {
                ret.Add(Nodes[connection.toNode]);
            }
        }

        return ret;
    }

    public int GetIndexOfNode(DialogueGraphNode node)
    {
        int i = -1;
        foreach (var node2 in Nodes)
        {
            i += 1;
            if (node.DNodeId == node2.DNodeId)
            {
                return i;
            }
        }

        return -1;
    }

    public List<DialogueGraphNode> GetContinuationsForNode(DialogueGraphNode node)
    {
        var loc = GetIndexOfNode(node);
        var conns = GetNodeConnections(loc);

        return conns;
    }
}
