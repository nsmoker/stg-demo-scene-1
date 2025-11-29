using Godot;

[Tool]
[GlobalClass]

public abstract partial class DialogueCondition : Resource
{
    public abstract bool Evaluate();
}
