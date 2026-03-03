using Godot;
using STGDemoScene1.Scripts.Items;

namespace STGDemoScene1.Scripts.Controls;

public partial class InventoryButton : TextureButton
{
    [Export]
    public ItemType ItemCategory;

    [Export]
    public Texture2D NoneTexture;

    [Export]
    public Texture2D NoneHoverTexture;

    public void SetItem(Item item)
    {
        if (item.ItemType != ItemType.None)
        {
            TextureNormal = item.Icon;
            TexturePressed = item.OutlineIcon;
            TextureHover = item.OutlineIcon;
        }
        else
        {
            TextureNormal = NoneTexture;
            TexturePressed = NoneHoverTexture;
            TextureHover = NoneHoverTexture;
        }
    }
}
