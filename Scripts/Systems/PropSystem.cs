using Godot;
using STGDemoScene1.Scripts.Resources;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class PropSystem
{
    private static readonly Dictionary<string, Prop> s_instanceMap = [];

    public static void Register(PropData data, Prop prop) => s_instanceMap[data.ResourcePath] = prop;

    public static void Instantiate(PropData data, Vector2 position)
    {
        Node scene = (Godot.Engine.GetMainLoop() as SceneTree)
            .CurrentScene;
        var prop = data.BaseScene.Instantiate<Prop>();
        scene.AddChild(prop);
        prop.SetSprite(data.Sprite);
        prop.Name = data.Name;
        prop.GlobalPosition = position;
        Register(data, prop);
    }

    public static void Unregister(PropData data) => s_instanceMap.Remove(data.ResourcePath);

    public static Prop GetInstance(PropData data) => s_instanceMap[data.ResourcePath];

    public static void ClearProp(PropData data)
    {
        s_instanceMap[data.ResourcePath].QueueFree();
        Unregister(data);
    }
}
