using Godot;
using System;

[Tool]
[GlobalClass]
public abstract partial class DialogueAction : Resource
{
    public abstract void Execute();
}
