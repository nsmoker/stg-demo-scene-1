using Godot;
using System.Collections.Generic;

namespace ArkhamHunters.Scripts;

public partial class ContainerDisplay : ItemListDisplay
{
    private Button _getAllButton;

    private string _containerEntity = "";
    public string ContainerEntity
    {
        get => _containerEntity;
        set
        {
            _containerEntity = value;
            if (_containerEntity != "")
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

    public bool GetAllPressed() => _getAllButton.IsPressed();

    public void OnInventoryChangeEvent(string entity, Item item, bool added)
    {
        if (entity == ContainerEntity)
        {
            DisplayItemList(entity);
        }
    }
}
