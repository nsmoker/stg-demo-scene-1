using System.Collections.Generic;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class ContainerDisplay: ItemListDisplay
{
    private Button _getAllButton;

    private ulong _containerEntity = 0;
    public ulong ContainerEntity
    {
        get { return _containerEntity; }
        set
        {
            _containerEntity = value;
            if (_containerEntity != 0)
            {
                DisplayItemList(ContainerEntity);
            }
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        InventorySystem.InventoryChangeHandlers += OnInventoryChangeEvent;
        _getAllButton = GetNode<Button>("VBoxContainer/GetAllButton");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (GetAllPressed())
        {
            Visible = false;
        }
    }
    
    public bool GetAllPressed()
    {
        return _getAllButton.IsPressed();
    }

    public void OnInventoryChangeEvent(ulong entity, Item item, bool added)
    {
        if (entity == ContainerEntity)
        {
            DisplayItemList(entity);
        }
    }
}