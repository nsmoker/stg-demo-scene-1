using Godot;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Addons.Edi.Scripts;

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

    private List<DialogueGraphNode> GetNodeConnections(int i)
    {
        List<DialogueGraphNode> ret = [];
        ret.AddRange(from connection in Connections where connection.FromNode == i select Nodes[connection.ToNode]);
        return ret;
    }

    private int GetIndexOfNode(DialogueGraphNode node)
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
