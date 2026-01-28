using Godot;
using System;

[Tool]
[GlobalClass]
public partial class PropData : Resource
{
    [Export]
    public string Name;

    [Export]
    public Texture2D Sprite;

    [Export]
    public PackedScene BaseScene;
}
