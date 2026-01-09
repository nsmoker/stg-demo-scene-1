using System.Collections.Generic;
using Godot;

public static class PropSystem
{
    private static Dictionary<string, Prop> _instanceMap = [];

    public static void Register(PropData data, Prop prop)
    {
        _instanceMap[data.ResourcePath] = prop;
    }

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

    public static void Unregister(PropData data)
    {
        _instanceMap.Remove(data.ResourcePath);
    }

    public static Prop GetInstance(PropData data)
    {
        return _instanceMap[data.ResourcePath];
    }

    public static void ClearProp(PropData data)
    {
        _instanceMap[data.ResourcePath].QueueFree();
        Unregister(data);
    }
}