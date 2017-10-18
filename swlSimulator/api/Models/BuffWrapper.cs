﻿using swlSimulator.api.Spells;
using swlSimulator.api.Weapons;

namespace swlSimulator.api.Models
{
    public class BuffWrapper
    {
        private readonly Player _player;

        public BuffWrapper(Player player)
        {
            _player = player;
        }

        public IBuff Exposed => _player.GetBuffFromName("Exposed");
        public IBuff OpeningShot => _player.GetBuffFromName("OpeningShot");
        public IBuff Savagery => _player.GetBuffFromName("Savagery");
        public IBuff UnstoppableForce => _player.GetBuffFromName("UnstoppableForce");

        // Hammer APL specifics
        public bool Enraged => _player.Rage >= 50 || _player.GetWeaponFromType(WeaponType.Hammer) is Hammer hammer &&
                               hammer.PneumaticAvailable;

        public bool FastAndFurious => _player.GetWeaponFromType(WeaponType.Hammer) is Hammer
                                          hammer && hammer.FastAndFuriousBonus;

        // Pistol APL specifics
        public bool RedChambers => _player.GetWeaponFromType(WeaponType.Pistol) is Pistol pistol &&
                                   pistol.LeftChamber == Chamber.Red && pistol.LeftChamber == pistol.RightChamber;

        public bool BlueChambers => _player.GetWeaponFromType(WeaponType.Pistol) is Pistol pistol &&
                                    pistol.LeftChamber == Chamber.Blue && pistol.LeftChamber == pistol.RightChamber;

        public bool WhiteChambers => _player.GetWeaponFromType(WeaponType.Pistol) is Pistol pistol &&
                                     pistol.LeftChamber == Chamber.White && pistol.LeftChamber == pistol.RightChamber;

        // Fist APL specifics
        public bool dd => _player.GetWeaponFromType(WeaponType.Fist) is Fist fist && fist.AllowFrenziedWrathAbilities;

        // TODO: Need to add all buffs here if we cant solve it in another way..
    }
}