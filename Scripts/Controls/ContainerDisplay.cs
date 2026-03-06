using Godot;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Controls;

public partial class ContainerDisplay : ItemListDisplay
{
    private Button _getAllButton;

    private string _containerEntity = "";

    private string ContainerEntity
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

    private bool GetAllPressed() => _getAllButton.IsPressed();

    private void OnInventoryChangeEvent(string entity, Item _1, bool _2)
    {
        if (entity == ContainerEntity)
        {
            DisplayItemList(entity);
        }
    }
}
