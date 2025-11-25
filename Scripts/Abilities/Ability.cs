using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts.Abilities
{
    [GlobalClass]

    public partial class Ability : Resource
    {
        [Export]
        public string Name;

        [Export]
        public string Description;

        [Export]
        public int Cooldown;

        [Export]
        public int Cost;

        [Export]
        public string IconPath;

        [Export]
        public bool IsPassive;

        [Export]
        public DamageRoll TargetDamage = new();

        [Export]
        public DamageRoll TargetHealing = new();

        [Export]
        public int AreaRadius;

        [Export]
        public DamageRoll AreaDamage = new();

        [Export]
        public DamageRoll AreaHealing = new();

        [Export]
        public Texture2D Icon;

        [Export]
        public Texture2D OutlineIcon;

        [Export]
        public bool IsMelee;

        [Export]
        public float Range;

        [Export]
        public bool RollsToHit;

        [Export]
        public int ToHitMod;
    }

}   