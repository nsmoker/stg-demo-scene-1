using Godot;

// TODO: Make common base class for dialogue scripts.
[Tool]
[GlobalClass]

public partial class DialogueCondition : Resource
{
    public virtual bool Evaluate()
    {
        return true;
    }
}
