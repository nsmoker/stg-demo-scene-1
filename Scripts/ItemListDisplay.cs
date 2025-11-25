using System.Collections.Generic;
using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class ItemListDisplay : PanelContainer
{
    protected readonly List<ItemDisplay> _displayedItems = new();
    
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
    
    public void DisplayItemList(List<Item> items)
    {
        Visible = true;
        foreach (var display in _displayedItems)
        {
            display.QueueFree();
        }
        
        _displayedItems.Clear();

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

    public bool ClosePressed()
    {
        return _closeButton.IsPressed();
    }

    private void OnChildSelected(Item item)
    {
        OnItemSelected?.Invoke(item);
    }
}