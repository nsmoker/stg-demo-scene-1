using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class Container: StaticBody2D, IInteractable, IContainerInteractable
{
    [Export]
    public ContainerData ContainerData;

    private Sprite2D _badgeSprite;
    private Sprite2D _closedSprite;
    private Sprite2D _openedSprite;
    

    public override void _Ready()
    {
        InventorySystem.SetInventory(ContainerData.ResourcePath, [.. ContainerData.StartingItems]);
        SetCollisionLayer(1 | (1 << 20));
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
        _closedSprite = GetNode<Sprite2D>("ClosedSprite");
        _openedSprite = GetNode<Sprite2D>("OpenedSprite");
    }

    public void SetShowBadge(bool showBadge)
    {
        _badgeSprite.Visible = showBadge;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Container;
    }

    public List<Item> GetItems()
    {
        _closedSprite.Visible = false;
        _openedSprite.Visible = true;
        return InventorySystem.RetrieveInventory(ContainerData.ResourcePath);
    }

    public void RemoveItem(Item item)
    {
        InventorySystem.RemoveItem(ContainerData.ResourcePath, item);
    }

    public void ClearItems()
    {
        InventorySystem.Remove(ContainerData.ResourcePath);
    }
}