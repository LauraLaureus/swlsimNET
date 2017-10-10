using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using swlsimNET.Models;
using swlsimNET.ServerApp;
using swlsimNET.ServerApp.Combat;
using swlsimNET.ServerApp.Models;
using swlsimNET.ServerApp.Spells;
using swlsimNET.ServerApp.Spells.Hammer;
using swlsimNET.ServerApp.Weapons;

namespace swlsimNET.Tests
{
    [TestClass]
    public class RifleTest
    {
        [TestMethod]
        public void TestRifleGimmick()
        {
            var setting = new Settings
            {
                PrimaryWeapon = WeaponType.Rifle,
                SecondaryWeapon = WeaponType.Fist,
                FightLength = 3,
                TargetType = TargetType.Champion,
                PrimaryWeaponProc = WeaponProc.Ksr43,
                Apl = "Rifle.RifleGrenadeSpell\r\n" +
                      "Rifle.RifleLoadGrenadeSpell"
            };

            var spell = new RifleGrenadeSpell();
            var spell2 = new RifleLoadGrenadeSpell();
            var player = new Player(setting);
            player.Spells.Add(spell);
            player.Spells.Add(spell2);

            var engine = new Engine(setting);
            var fight = engine.StartFight(player);

            var endTime = fight.RoundResults.Last().TimeSec;
            var rounds = fight.RoundResults.Count;

            var loadCount = fight.RoundResults
                .SelectMany(r => r.Attacks.Where(a => a.Spell is RifleLoadGrenadeSpell)).Count();
            var grenadeCount = fight.RoundResults
                .SelectMany(r => r.Attacks.Where(a => a.Spell is RifleGrenadeSpell)).Count();

            // 0.0, start load cast
            // 1.0, finish load cast
            // 1.0, start cook grenade
            // 1.0, finish cook grenade
            // 1.0, grenade cast

            // 2.0, start next load cast
            // 3.0, finish load cast
            // 3.0, start cook grenade
            // 3.0, finish cook grenade
            // 3.0, grenade cast

            // 3.0, finish load cast
            // 3.0, start next load cast
            // 4.0, finish load cast
            // 4.0, start next load cast
            // 5.0, finish load cast
            // 5.0, start next load cast
            // 6.0, finish load cast
            // 6.0, cooking timer finished
            // 6.0, grenade cast

            // TODO: Fix this

            Assert.AreEqual(rounds, 5);
            Assert.AreEqual(endTime, 6.0m);
            Assert.IsTrue(loadCount == 6);
            Assert.IsTrue(grenadeCount == 1);
        }

        private sealed class RifleLoadGrenadeSpell : Spell
        {
            public RifleLoadGrenadeSpell()
            {
                WeaponType = WeaponType.Rifle;
                SpellType = SpellType.Cast;
                CastTime = 1.0m;
                BaseDamage = 1;
            }
        }

        private sealed class RifleGrenadeSpell : Spell
        {
            public RifleGrenadeSpell()
            {
                PrimaryGimmickCost = 1;
                WeaponType = WeaponType.Rifle;
                SpellType = SpellType.Cast;
                CastTime = 0;
                BaseDamage = 1;
            }
        }
    }
}