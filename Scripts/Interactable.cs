using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Items;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts;

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
