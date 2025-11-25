using System.Collections.Generic;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class ContainerDisplay: ItemListDisplay
{
    private Button _getAllButton;
    
    public override void _Ready()
    {
        base._Ready();
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
}