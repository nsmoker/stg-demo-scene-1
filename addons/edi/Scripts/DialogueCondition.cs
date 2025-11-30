using Godot;

[Tool]
[GlobalClass]

public partial class DialogueCondition : Resource
{
    public virtual bool Evaluate()
    {
        return true;
    }
}
