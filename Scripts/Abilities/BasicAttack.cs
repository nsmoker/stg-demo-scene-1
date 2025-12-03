using ArkhamHunters.Scripts.Items;
using Godot;

namespace ArkhamHunters.Scripts.Abilities
{
    [GlobalClass]

    public partial class BasicAttack : Ability
    {
        private Item _weapon;

        [Export]
        public Item Weapon
        {
            get => _weapon;
            set
            {
                if (value == null || value.WeaponStats == null)
                {
                    return;
                }
                SetWeapon(value);
            }
        }

        public void SetWeapon(Item weapon)
        {
            var weaponStats = weapon.WeaponStats;
            TargetDamage = weaponStats.DamageRolls;
            RollsToHit = true;
            IsMelee = weaponStats.Melee;
            Range = weaponStats.Range;
            ToHitMod = weaponStats.ToHitMod;
            Icon = weapon.Icon;
            OutlineIcon = weapon.OutlineIcon;
            Name = "Attack";
            Description = weapon.Description;
            Cooldown = 0;
            Cost = 0;
            IsPassive = false;
            RollsToHit = true;
        }
    }

}