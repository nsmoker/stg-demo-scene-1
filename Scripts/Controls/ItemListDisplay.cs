using Godot;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Controls;

public partial class ItemListDisplay : PanelContainer
{
    private readonly List<ItemDisplay> _displayedItems = [];

    private VBoxContainer _container;
    private Button _closeButton;

    [Export]
    public PackedScene ItemDisplayScene;

    public ItemSelected OnItemSelected;

    public override void _Ready()
    {
        _container = GetNode<VBoxContainer>("VBoxContainer");
        _closeButton = GetNode<Button>("VBoxContainer/CloseButton");
    }

    public override void _Process(double delta)
    {
        if (ClosePressed())
        {
            Visible = false;
        }
    }

    public void DisplayItemList(string entity)
    {
        Visible = true;
        foreach (var display in _displayedItems)
        {
            display.QueueFree();
        }

        _displayedItems.Clear();

        var items = InventorySystem.RetrieveInventory(entity);

        foreach (var item in items)
        {
            var newDisplay = ItemDisplayScene.Instantiate<ItemDisplay>();
            newDisplay.OnItemSelected += OnChildSelected;
            _container.AddChild(newDisplay);
            _container.MoveChild(newDisplay, 0);
            newDisplay.DisplayItem(item);
            _displayedItems.Add(newDisplay);
        }
    }

    private bool ClosePressed() => _closeButton.IsPressed();

    private void OnChildSelected(Item item) => OnItemSelected?.Invoke(item);
}
