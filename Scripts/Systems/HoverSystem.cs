using Godot;
using System;

// TODO: DON'T SHIP
public static class HoverSystem
{
    private static string _hovered = null;

    public static string Hovered { get { return _hovered; } }

    public static void SetHovered(string hovered)
    {
        _hovered = hovered;
    }

    public static void SetUnhovered(string unhovered)
    {
        if (_hovered != null && _hovered.Equals(unhovered))
        {
            _hovered = null;
        }
    }

    public static bool AnyHovered()
    {
        return _hovered != null;
    }

    public static bool IsCharacterHovered()
    {
        return CharacterSystem.GetInstance(_hovered) != null;
    }
}
