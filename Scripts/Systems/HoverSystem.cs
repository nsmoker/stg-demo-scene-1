using Godot;
using System;

// TODO: DON'T SHIP
public static class HoverSystem
{
    public static string Hovered { get; private set; } = null;

    public static void SetHovered(string hovered) => Hovered = hovered;

    public static void SetUnhovered(string unhovered)
    {
        if (Hovered != null && Hovered.Equals(unhovered))
        {
            Hovered = null;
        }
    }

    public static bool AnyHovered() => Hovered != null;

    public static bool IsCharacterHovered() => CharacterSystem.GetInstance(Hovered) != null;
}
