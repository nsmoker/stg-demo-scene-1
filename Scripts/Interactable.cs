using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;

namespace ArkhamHunters.Scripts;

public enum InteractionType
{
    Dialogue,
    Container,
    Toggleable,
    Furniture,
}

public interface IInteractable
{
    void SetShowBadge(bool showBadge);
    InteractionType GetInteractionType();
}

public interface IDialogueInteractable
{
    Conversation GetDialogue();
    int GetEntryPoint();
}

public interface IContainerInteractable
{
    List<Item> GetItems();
}

public interface IToggleableInteractable
{
    void Toggle();
}
