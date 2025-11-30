using Godot;

// TODO: Make common base class for dialogue scripts.
[Tool]
[GlobalClass]

public partial class DialogueCondition : Resource
{
    protected static Player GetPlayerNode()
    {
        Node root = ((SceneTree) Godot.Engine.GetMainLoop()).CurrentScene;
        return root.GetNode<Player>("Player");
    }

    public virtual bool Evaluate()
    {
        return true;
    }
}
