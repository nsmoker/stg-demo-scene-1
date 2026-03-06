using Godot;

namespace STGDemoScene1.Addons.Edi.Scripts;

[Tool]
[GlobalClass]
public partial class DialogueConnection : Resource
{
    [Export]
    public int FromNode;

    [Export]
    public int ToNode;
}
