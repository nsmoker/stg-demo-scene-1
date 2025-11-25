using Godot;
using ArkhamHunters.Scripts.Items;

namespace ArkhamHunters.Scripts.Items
{
    public delegate void ItemSelected(Item displayedItem);
    
    public enum ItemType
    {
        Weapon,
        Armor,
        Wearable,
        Consumable,
        None
    }

    public enum WeaponProficiency
    {
        Shank0,
        Shank1,
        Shank2,
        Guns0,
        Guns1,
        Guns2,
        Staff0,
        Staff1,
        Staff2,
    }

    public enum DamageType
    {
        D4,
        D6,
        D8,
        D10,
        D12
    }
}

[GlobalClass]
public partial class Item : Resource
{
    [Export]
    public ItemType ItemType { get; private set; }
    
    [Export]
    public AttributeRequirement AttributeRequirements = new();
    [Export]
    public SkillRequirement SkillRequirements = new();

    [Export] 
    public ConsumableStats ConsumableStats { get; private set; } = new();

    [Export] 
    public WearableStats WearableStats { get; private set; } = new();

    [Export] 
    public ArmorStats ArmorStats { get; private set; } = new();

    [Export] 
    public WeaponStats WeaponStats { get; private set; } = new();

    private string _name;

    // TODO: EVIL. Should be a clearer way to set a particular instance of an Item as equipped instead of relying on the boxing.
    [Export]
    public string Name
    {
        get
        {
            if (Equipped)
            {
                return _name + " (Equipped)";
            }
            else
            {
                return _name;
            }
        }
        set => _name = value;
    }
    [Export] 
    public string Description;

    [Export] 
    public Texture2D Icon;
    
    [Export]
    public Texture2D OutlineIcon;

    public bool Equipped = false;

    public static Item NoneItem()
    {
        var ret = new Item();
        ret.ItemType = ItemType.None;
        return ret;
    }
}