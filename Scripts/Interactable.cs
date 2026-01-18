using System;
using System.Collections.Generic;
using ArkhamHunters.Scripts;
using Godot;

namespace ArkhamHunters.Scripts;

public enum InteractionType
{
    Dialogue,
    Container,
    Toggleable,
    Furniture,
    Trigger,
}

public interface IInteractable
{
    public void SetShowBadge(bool showBadge);
    public InteractionType GetInteractionType();
}

public interface IDialogueInteractable
{
    public Conversation GetDialogue();
    public int GetEntryPoint();
}

public interface IContainerInteractable
{
    public List<Item> GetItems();
}

public interface IToggleableInteractable
{
    public void Toggle();
}

public interface ITriggerInteractable
{
    public void Trigger();
}