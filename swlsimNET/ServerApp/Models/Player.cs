﻿using System.Collections.Generic;
using System.Linq;
using swlsimNET.ServerApp.Combat;
using swlsimNET.ServerApp.Spells;
using swlsimNET.ServerApp.Spells.Buffs;
using swlsimNET.ServerApp.Spells.Items;
using swlsimNET.ServerApp.Utilities;
using swlsimNET.ServerApp.Weapons;

namespace swlsimNET.ServerApp.Models
{

    public interface IPlayer
    {
        double CombatPower { get; }
        double GlanceReduction { get; } // hit
        double CriticalChance { get; }
        double CritPower { get; }
        double BasicSignetBoost { get; }
        double PowerSignetBoost { get; }
        double EliteSignetBoost { get; }
        int Interval { get; }
        int CurrentTimeMs { get; }

        List<ISpell> Spells { get; }
        List<IBuff> Buffs { get; }
        List<IBuff> AbilityBuffs { get; }

        IBuff GetBuffFromName(string name);
        IBuff GetAbilityBuffFromName(string name);
        bool HasPassive(string name);
        Passive GetPassive(string name);

        Weapon PrimaryWeapon { get; }
        Weapon SecondaryWeapon { get; }

        Weapon GetWeaponFromSpell(ISpell spell);
        Weapon GetOtherWeaponFromSpell(ISpell spell);
        Weapon GetWeaponFromType(WeaponType wtype);

        double GetWeaponResourceFromType(WeaponType wtype);

        void AddBonusAttack(RoundResult rr, ISpell spell);
    }


    public interface ICombat
    {
        int CastTime { get; set; }
        //int ChannelTime { get; set; }
        int CurrentTimeMs { get; }
        int GCD { get; set; }
        int RepeatHits { get; }
        Spell CurrentSpell { get; }
        RoundResult NewRound(int currentMs, int pingMs);
    }


    public class Player : IPlayer, ICombat
    {
        private bool _passivesInitiated;

        public Player(Weapon primaryWeapon, Weapon secondaryWeapon, List<Passive> passives)
        {
            PrimaryWeapon = primaryWeapon;
            SecondaryWeapon = secondaryWeapon;
            Passives = passives;

            Buffs = new List<IBuff>();
            {
                //TODO: Fix.
                //if (S.Default.Exposed) Buffs.Add(new Exposed());
                //if (S.Default.OpeningShot) Buffs.Add(new OpeningShot());
                //if (S.Default.Savagery) Buffs.Add(new Savagery());
            }

            AbilityBuffs = new List<IBuff>();
            InitAbilityBuffs();

            this.Buff = new BuffWrapper(this);
        }

        private void InitAbilityBuffs()
        {
            #region Blade

            //AbilityBuffs.Add(new Spells.Blade.Buffs.SupremeHarmony()); TODO: Fix

            #endregion

            #region Blood



            #endregion

            #region Chaos



            #endregion

            #region Elemental



            #endregion

            #region Fist

            AbilityBuffs.Add(new Spells.Fist.Buffs.Savagery());

            #endregion

            #region Hammer

            //AbilityBuffs.Add(new Spells.Hammer.Buffs.UnstoppableForce());  //TODO: Fix

            #endregion

            #region Pistol



            #endregion

            #region AssaultRifle



            #endregion

            #region Shotgun



            #endregion
        }

        private void InitPassives()
        {
            // Go through all passives
            foreach (var passive in Passives)
            {
                passive.Init(this);

                if (passive.ModelledInWeapon) continue; ;

                passive.LoopSpellsFromPassive(this);

                // Any sub passives
                foreach (var subPassive in passive.SpecificSpellTypes)
                {
                    subPassive.LoopSpellsFromPassive(this);
                }

                if (!passive.SpecificWeaponTypeBonus) continue;

                // The passive modifies all spells of the same weapon type
                var allSpells = Spells.Where(s => s.WeaponType == passive.WeaponType);
                foreach (var s in allSpells)
                {
                    passive.ModifySpellWithPassive(s);
                }
            }

            _passivesInitiated = true;
        }

        public RoundResult NewRound(int currentMs, int interval)
        {
            if (!_passivesInitiated) InitPassives();

            Interval = interval;
            CurrentTimeMs = currentMs;

            var rr = new RoundResult
            {
                TimeMs = currentMs,
                Interval = interval
            };

            // TODO: Check order, e.g. should passive bonus spell get bonus from buff cast same round?
            // Order of a round
            PreRound(rr);
            StartRoundBuffs();
            WeaponPreAttack(rr);
            ExecuteAction(rr);
            ExecuteBuff(rr);
            ItemProccs(rr);
            WeaponAfterAttack(rr);
            PassiveBonusSpells(rr);
            EndRound(rr);
            EndRoundBuffs(rr);
            PostRound(rr);

            return rr;
        }

        private void PreRound(RoundResult rr)
        {
            rr.PrimaryEnergyStart = PrimaryWeapon.Energy;
            rr.SecondaryEnergyStart = SecondaryWeapon.Energy;
            rr.PrimaryGimmickStart = PrimaryWeapon.GimmickResource;
            rr.SecondaryGimmickStart = SecondaryWeapon.GimmickResource;
        }

        private void StartRoundBuffs()
        {
            var availableBuffs = Buffs.Where(b => b.CanActivate());

            foreach (var buff in availableBuffs)
            {
                buff.Activate();
            }
        }

        private void WeaponPreAttack(RoundResult rr)
        {
            PrimaryWeapon.PreAttack(this, rr);
            SecondaryWeapon.PreAttack(this, rr);
        }

        private void ExecuteAction(RoundResult rr)
        {
            Attack attack;

            if (CurrentSpell == null)
            {
                // Get first spell from top of priority list we can execute
                var spell = Spells.FirstOrDefault(s => s.CanExecute(this));

                // If spell is null we cant cast anything
                if (spell == null) return;

                // Specific Hammer stuff, if enraged get rage spell
                if (Enraged)
                {
                    var rageSpell = Spells.Find(s => s.Name == spell.Name + "Rage");
                    if (rageSpell != null)
                    {
                        spell = rageSpell;
                    }
                }

                attack = spell.Execute(this);

                // If attack is null we have started a cast
                if (attack != null)
                {
                    rr.Attacks.Add(attack);
                }
            }
            else
            {
                // We are already casting
                attack = CurrentSpell.Continue(this);

                // If attack is null cast is not complete
                if (attack != null)
                {
                    rr.Attacks.Add(attack);
                }
            }
        }

        private void ExecuteBuff(RoundResult rr)
        {
            var attack = rr.Attacks.FirstOrDefault();
            if (attack?.Spell.AbilityBuff == null) return;

            var buff = Buffs.Find(b => b == attack.Spell.AbilityBuff);
            buff.Activate();
        }

        private void ItemProccs(RoundResult rr)
        {
            var attack = rr.Attacks.FirstOrDefault();
            if (attack == null || !attack.IsHit || attack.Damage <= 0) return;

            var weapon = GetWeaponFromSpell(attack.Spell);
            if (weapon == null) return;

            if (attack.IsCrit)
            {
                // Cold Silver Dice 22% on Crit +1 Energy.
                if (Helper.RNG() >= 0.78)
                {
                    weapon.Energy++;
                    var bonusAttack = ExecuteNoGCD(new ColdSilver(this));
                    rr.Attacks.Add(bonusAttack);
                }
            }

            // Hit 11% (<50% BossHP) +1 Energy.
            if (Helper.RNG() >= 0.945)
            {
                weapon.Energy++;
                var bonusAttack = ExecuteNoGCD(new SeedOfAggression(this));
                rr.Attacks.Add(bonusAttack);
            }

            // Ashes Proc from Spells dealing X*CombatPower (NOT ON GCD)
            if (RepeatHits == 3)
            {
                var bonusAttack = ExecuteNoGCD(new Ashes(this));
                rr.Attacks.Add(bonusAttack);

                RepeatHits = 0;
            }
            else if (RepeatHits < 3)
            {
                RepeatHits++;
            }
        }

        private void WeaponAfterAttack(RoundResult rr)
        {
            var attack = rr.Attacks.FirstOrDefault();
            if (attack == null || !attack.IsHit) return;

            var weapon = GetWeaponFromSpell(attack.Spell);
            weapon?.AfterAttack(this, attack.Spell, rr);
            weapon?.WeaponAffixes(this, attack.Spell, rr);
        }

        private void PassiveBonusSpells(RoundResult rr)
        {
            var attack = rr.Attacks.FirstOrDefault();
            if (attack == null || !attack.IsHit) return;

            var spell = attack.Spell;
            if (spell?.PassiveBonusSpell != null)
            {
                if (spell.PassiveBonusSpell.BonusSpellOnlyOnCrit)
                {
                    // Passive bonus spell only on crit
                    if (attack.IsCrit)
                    {
                        AddBonusAttack(rr, spell.PassiveBonusSpell);
                    }
                }
                else
                {
                    // Passive bonus spell on hit
                    AddBonusAttack(rr, spell.PassiveBonusSpell);
                }
            }
        }

        private void EndRound(RoundResult rr)
        {
            var attack = rr.Attacks.FirstOrDefault();

            if (attack != null && attack.IsHit)
            {
                var weapon = GetWeaponFromSpell(attack.Spell);

                if (attack.IsCrit)
                {
                    // Energy on crit 1s IDC
                    weapon?.EnergyOnCrit(this);
                }
            }

            foreach (var a in rr.Attacks)
            {
                // TODO: Check if this is correct GREM, (non damage shit never added to report totals)
                if (a.IsHit && a.Damage > 0) rr.TotalHits++;
                if (a.IsCrit && a.Damage > 0) rr.TotalCrits++;
                rr.TotalDamage += a.Damage;
            }
        }

        private void EndRoundBuffs(RoundResult rr)
        {
            var attack = rr.Attacks.FirstOrDefault();
            var abilityBuff = attack?.Spell.AbilityBuff;

            foreach (var buff in Buffs)
            {
                if (buff == abilityBuff) continue;

                if (buff.Duration >= 0)
                    buff.Duration -= rr.Interval;
                if (buff.Cooldown > 0)
                    buff.Cooldown -= rr.Interval;

                if (buff is AbilityBuff ab)
                {
                    var weapon = GetWeaponFromType(ab.WeaponType);
                    if (weapon == null) continue;

                    if (ab.Active && ab.Duration % 1000 == 0)
                    {
                        weapon.GimmickResource += ab.GimmickGainPerSec;
                        weapon.Energy += ab.EnergyGainPerSec;
                    }
                }
            }
        }

        private void PostRound(RoundResult rr)
        {
            // +1 resource per sec primary weapon
            if (rr.TimeMs != 0 && rr.TimeMs % 1000 == 0)
            {
                PrimaryWeapon.Energy++;
            }

            // +1 resource every other second secondary weapon
            if (rr.TimeMs != 0 && rr.TimeMs % 2000 == 0)
            {
                SecondaryWeapon.Energy++;
            }

            // Lower cooldown of all spells except the one used this round
            foreach (var spell in Spells)
            {
                if (spell.Name == rr.Attacks.FirstOrDefault()?.Spell.Name) continue;

                if (spell.Cooldown > 0) spell.Cooldown -= rr.Interval;
            }

            // Lower GCD
            if (GCD > 0) GCD -= rr.Interval;

            // Lower CastTime
            if (CastTime > 0) CastTime -= rr.Interval;

            // Add relevant info to round result
            rr.PrimaryEnergyEnd = PrimaryWeapon.Energy;
            rr.SecondaryEnergyEnd = SecondaryWeapon.Energy;
            rr.PrimaryGimmickEnd = PrimaryWeapon.GimmickResource;
            rr.SecondaryGimmickEnd = SecondaryWeapon.GimmickResource;
        }

        private Attack ExecuteNoGCD(ISpell spell)
        {
            return spell.Execute(this);
        }

        public void AddBonusAttack(RoundResult rr, ISpell spell)
        {
            rr.Attacks.Add(ExecuteNoGCD(spell));
        }

        public Weapon GetWeaponFromSpell(ISpell spell)
        {
            if (PrimaryWeapon.WeaponType == spell.WeaponType)
                return PrimaryWeapon;
            return SecondaryWeapon.WeaponType == spell.WeaponType ? SecondaryWeapon : null;
        }

        public Weapon GetOtherWeaponFromSpell(ISpell spell)
        {
            // Can go really wrong if APL contains 3 weapon types
            return PrimaryWeapon.WeaponType != spell.WeaponType ? PrimaryWeapon : SecondaryWeapon;
        }

        public Weapon GetWeaponFromType(WeaponType wtype)
        {
            if (PrimaryWeapon.WeaponType == wtype)
                return PrimaryWeapon;
            return SecondaryWeapon.WeaponType == wtype ? SecondaryWeapon : null;
        }

        public double GetWeaponResourceFromType(WeaponType wtype)
        {
            if (PrimaryWeapon.WeaponType == wtype)
                return PrimaryWeapon.GimmickResource;
            return SecondaryWeapon.WeaponType == wtype ? SecondaryWeapon.GimmickResource : 0;
        }

        public IBuff GetBuffFromName(string name)
        {
            var buffs = Buffs.Where(b => b.Name == name).ToList();
            if (buffs.Count == 1) return buffs.FirstOrDefault();

            // If there is several whith same name get abilitybuff
            var abilitybuff = buffs.Find(b => b is AbilityBuff);
            return abilitybuff;
        }

        public IBuff GetAbilityBuffFromName(string name)
        {
            var abilitybuff = AbilityBuffs.Find(b => b.Name == name && b is AbilityBuff);

            // Shameful add to Buffs list to keep other stuff working for now
            Buffs.Add(abilitybuff);
            return abilitybuff;
        }

        public bool HasPassive(string name)
        {
            return Passives.Any(p => p.Name == name);
        }

        public Passive GetPassive(string name)
        {
            return Passives.Find(p => p.Name == name);
        }

        #region Properties

        #region IPlayer implementation

        // Implements IPlayer TODO: Get from Frontend.
        public double CombatPower { get; protected set; } = 1200;

        public double GlanceReduction { get; protected set; } = 0.3;
        public double CriticalChance { get; protected set; } = 0.1;
        public double CritPower { get; set; } = 2.3;
        public double BasicSignetBoost { get; protected set; } = 1.74;
        public double PowerSignetBoost { get; protected set; } = 1.18;
        public double EliteSignetBoost { get; protected set; } = 1.43;
        public int Interval { get; set; }
        public List<ISpell> Spells { get; set; }
        public List<IBuff> Buffs { get; }
        public List<IBuff> AbilityBuffs { get; }
        public List<Passive> Passives { get; set; }
        public Weapon PrimaryWeapon { get; set; }
        public Weapon SecondaryWeapon { get; set; }

        #endregion

        #region ICombat implementation

        // Implements ICombat
        public int CastTime { get; set; }
        public int CurrentTimeMs { get; private set; }
        public int GCD { get; set; }
        public int RepeatHits { get; private set; }
        public Spell CurrentSpell { get; set; }

        #endregion

        #region APL defines

        // Hammer APL specifics
        public bool Enraged => Rage >= 50;
        public bool FastAndFurious => GetWeaponFromType(WeaponType.Hammer) is Hammer
            hammer && hammer.FastAndFuriousBonus;

        // Buffs
        public BuffWrapper Buff { get; }

        // Weapon gimmick resource (for APL)
        public double Chi => GetWeaponResourceFromType(WeaponType.Blade);
        public double Corruption => GetWeaponResourceFromType(WeaponType.Blood);
        public double Fury => GetWeaponResourceFromType(WeaponType.Fist);
        public double Heat => GetWeaponResourceFromType(WeaponType.Elemental);
        public double Paradox => GetWeaponResourceFromType(WeaponType.Chaos);
        public double Rage => GetWeaponResourceFromType(WeaponType.Hammer);
        public double Shells => GetWeaponResourceFromType(WeaponType.Shotgun);

        // Weapon wrappers (for APL)
        public Weapon Blade => GetWeaponFromType(WeaponType.Blade);
        public Weapon Blood => GetWeaponFromType(WeaponType.Blood);
        public Weapon Chaos => GetWeaponFromType(WeaponType.Chaos);
        public Weapon Elemental => GetWeaponFromType(WeaponType.Elemental);
        public Weapon Fist => GetWeaponFromType(WeaponType.Fist);
        public Weapon Hammer => GetWeaponFromType(WeaponType.Hammer);
        public Weapon Pistol => GetWeaponFromType(WeaponType.Pistol);
        public Weapon Rifle => GetWeaponFromType(WeaponType.AssaultRifle);
        public Weapon Shotgun => GetWeaponFromType(WeaponType.Shotgun);

        #endregion

        #endregion

        // TODO: Fix this...
        // Only done since buffs and spells can have same
        // Expressions engine does not like this
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

            // TODO: Need to add all buffs here if we cant solve it in another way..
        }
    }
}