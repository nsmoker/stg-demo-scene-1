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

    public string EquipmentId;

    private string _currentEntity = "";
    public string CurrentEntity 
    {
        get => _currentEntity;
        set
        {
            _currentEntity = value;
            _itemListDisplay.DisplayItemList(_currentEntity);
        }
    }

    private InventoryButton _armorButton;
    private InventoryButton _weaponButton;
    private InventoryButton _helmetButton;

    private Label _attModLabel;
    private Label _skillModLabel;
    
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

    public ItemSelected OnItemSelected;

    public override void _Ready()
    {
        _buttons.Add(GetNode<InventoryButton>("ArmorButton"));
        _buttons.Add(GetNode<InventoryButton>("HelmetButton"));
        _buttons.Add(GetNode<InventoryButton>("WeaponButton"));
        _itemListDisplay = GetNode<ItemListDisplay>("ItemListDisplay");
        _itemListDisplay.OnItemSelected += OnSelection;

        _armorButton = GetNode<InventoryButton>("ArmorButton");
        _weaponButton = GetNode<InventoryButton>("WeaponButton");
        _helmetButton = GetNode<InventoryButton>("HelmetButton");

        _attModLabel = GetNode<Label>("StatDisplayBox/AttMods");
        _skillModLabel = GetNode<Label>("StatDisplayBox/SkillMods");

        EquipmentSystem.EquipmentChangeHandlers += OnEquipSetChanged;
        InventorySystem.InventoryChangeHandlers += OnInventoryChanged;
    }

    public override void _Process(double delta)
    {
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

    private void OnEquipSetChanged(string id, EquipmentSet equipmentSet)
    {
        if (id == EquipmentId)
        {
            _armorButton.SetItem(equipmentSet.Armor);
            _weaponButton.SetItem(equipmentSet.Weapon);
            _helmetButton.SetItem(equipmentSet.Helmet);

            _attModLabel.Text = BuildAttModDesc(equipmentSet.ComputeAttributeBonus());
            _skillModLabel.Text = BuildSkillModDesc(equipmentSet.ComputeSkillBonus());
        }
    }

    private void OnInventoryChanged(string entity, Item item, bool added)
    {
        if (entity == CurrentEntity)
        {
            _itemListDisplay?.DisplayItemList(CurrentEntity);
        }
    }
}