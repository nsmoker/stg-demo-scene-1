using Godot;

namespace STGDemoScene1.Addons.Edi.Scripts;

// TODO: Make common base class for dialogue scripts.
[Tool]
[GlobalClass]

public partial class DialogueCondition : Resource
{
    public virtual bool Evaluate() => true;
}
