using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class ItemDisplay: VBoxContainer
{
    private Label _nameLabel;
    private Label _descriptionLabel;
    private TextureRect _textureRect;
    
    private Item _displayedItem;
    
    public ItemSelected OnItemSelected;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("NameLabel");
        _descriptionLabel = GetNode<Label>("DescriptionLabel");
        _textureRect = GetNode<TextureRect>("TextureRect");

        base.MouseEntered += OnMouseEntered;
        base.MouseExited += OnMouseExited;
        base.GuiInput += InputHandler;
    }

    public void DisplayItem(Item item)
    {
        if (item.ItemType != ItemType.None)
        {
            _displayedItem = item;
            _textureRect.Texture = item.Icon;
            _nameLabel.Text = item.Name;
            _descriptionLabel.Text = item.Description;
        }
    }

    private void OnMouseEntered()
    {
        _textureRect.Texture = _displayedItem.OutlineIcon;
    }

    private void OnMouseExited()
    {
        _textureRect.Texture = _displayedItem.Icon;
    }

    private void InputHandler(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.GetButtonIndex() == MouseButton.Left)
        {
            OnItemSelected.Invoke(_displayedItem);
        }
    }
}