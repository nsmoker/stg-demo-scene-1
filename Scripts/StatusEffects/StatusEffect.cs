using Godot;
using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.StatusEffects;

[Tool]
[GlobalClass]
public partial class StatusEffect : Resource
{
    [Export]
    public string Name = "Status Effect";
    [Export]
    public bool Stacks;
    [Export]
    public bool IsPermanent;
    [Export]
    public int Duration;
    [Export]
    public Texture2D Icon;
    public virtual bool OnStackAdd(Character target) => true;

    public virtual bool OnStackRemove(Character target) => true;
}

