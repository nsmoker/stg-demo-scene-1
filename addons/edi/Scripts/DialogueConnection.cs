using Godot;
using System;

[Tool]
[GlobalClass]
public partial class DialogueConnection : Resource
{
    [Export]
    public int fromNode;

    [Export]
    public int toNode;

    public DialogueConnection() { }
}