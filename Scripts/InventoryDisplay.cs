using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts;

public partial class InventoryDisplay: Panel
{
    private readonly List<InventoryButton> _buttons = new();
    
    private ItemListDisplay _itemListDisplay;
    
    private EquipmentSet _equipmentSet;
    
    
    private List<Item> _currentInventory = new();
    public List<Item> CurrentInventory 
    {
        get => _currentInventory;
        set
        {
            _currentInventory = value;
            _itemListDisplay.DisplayItemList(_currentInventory);
        }
    }
    
    private string BuildPropDesc(string name, int val)
    {
        var sb = new StringBuilder();
        sb.Append($"{name}: ");
        if (val != 0)
        {
            sb.Append(val > 0 ? "+" : "-");
        }

        sb.Append($"{val}");
        return sb.ToString();
    }

    private string BuildSkillModDesc(SkillBonus skillMods)
    {
        if (skillMods.IsSignificant())
        {
            return $"Skill Modifiers: " +
                   $"{BuildPropDesc("Stealth", skillMods.StealthBonus)} "
                   + $"{BuildPropDesc("First Aid", skillMods.FirstAidBonus)} "
                   + $"{BuildPropDesc("Alchemy", skillMods.AlchemyBonus)} "
                   + $"{BuildPropDesc("Rhetoric", skillMods.RhetoricBonus)} "
                   + $"{BuildPropDesc("Mechanics", skillMods.MechanicsBonus)} ";
        }
        else
        {
            return "";
        }
    }

    private string BuildAttModDesc(AttributeBonus attributeMods)
    {
        if (attributeMods.IsSignificant())
        {
            return $"Attribute Modifiers: " +
                   $"{BuildPropDesc("STR", attributeMods.StrengthBonus)} "
                   + $"{BuildPropDesc("DEX", attributeMods.DexterityBonus)} "
                   + $"{BuildPropDesc("END", attributeMods.EnduranceBonus)} "
                   + $"{BuildPropDesc("CHA", attributeMods.CharismaBonus)} "
                   + $"{BuildPropDesc("INT", attributeMods.IntelligenceBonus)} "
                   + $"{BuildPropDesc("WIS", attributeMods.WisdomBonus)} "
                   + $"{BuildPropDesc("WIL", attributeMods.WillpowerBonus)} ";
        }
        else
        {
            return "";
        }
    }
    
    [Export]
    public EquipmentSet CurrentEquipment
    {
        get => _equipmentSet;
        set
        {
            _equipmentSet = value;
            GetNode<InventoryButton>("ArmorButton").SetItem(_equipmentSet.Armor);
            GetNode<InventoryButton>("WeaponButton").SetItem(_equipmentSet.Weapon);
            GetNode<InventoryButton>("HelmetButton").SetItem(_equipmentSet.Helmet);

            var attModLabel = GetNode<Label>("StatDisplayBox/AttMods");
            var skillModLabel = GetNode<Label>("StatDisplayBox/SkillMods");
            attModLabel.Text = BuildAttModDesc(_equipmentSet.ComputeAttributeBonus());
            skillModLabel.Text = BuildSkillModDesc(_equipmentSet.ComputeSkillBonus());
        } 
    }

    public ItemSelected OnItemSelected;

    public override void _Ready()
    {
        _buttons.Add(GetNode<InventoryButton>("ArmorButton"));
        _buttons.Add(GetNode<InventoryButton>("HelmetButton"));
        _buttons.Add(GetNode<InventoryButton>("WeaponButton"));
        _itemListDisplay = GetNode<ItemListDisplay>("ItemListDisplay");
        _itemListDisplay.OnItemSelected += OnSelection;
    }

    public override void _Process(double delta)
    {
        if (GetPressedCategory() != ItemType.None)
        {
            _itemListDisplay.DisplayItemList(
                CurrentInventory.Where(i => i.ItemType == GetPressedCategory()).ToList()
            );
        }
    }

    private ItemType GetPressedCategory()
    {
        foreach (var button in _buttons)
        {
            if (button.IsPressed())
            {
                return button.ItemCategory;
            }
        }
        
        return ItemType.None;
    }

    private void OnSelection(Item item)
    {
        OnItemSelected?.Invoke(item);
    }
}