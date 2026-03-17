using Game.Interfaces;
using Game.Math;
using Game.Skills;
using System;
using System.Collections.Generic;

namespace Game.Models
{
    public class Archer : Character, IShooter, ISkillUser
    {
        public event Action<Character, Character> OnCrit;
        public event Action<Character, Character, ISkill> OnSkillUsed;
        public Random rand { get; set; } = new Random();
        public List<ISkill> Skills { get; set; } = new List<ISkill>();

        public Archer(string name) : base(name)
        {
            HP = 200;
            CurrentHP = 200;
            EP = 100;
            CurrentEP = 100;
            PhysicDamage = 20;
            MagicDamage = 10;
            Class = "Archer";
            AttackType = "shoots";
            EnergyType = "SP";
            Skills.Add(new FireArrow());
        }

        public override void ShowInfo()
        {
            Console.WriteLine($"{Name} | Class: {Class} | HP: {CurrentHP}/{HP} | PhysicDamage: {PhysicDamage} | MagicDamage: {MagicDamage} | {EnergyType}: {CurrentEP}/{EP} | LV: {Level}");
        }

        public override void Attack(Character target)
        {
            double damage = PhysicDamage;
            if (rand.NextDouble() < 0.2)
            {
                OnCrit?.Invoke(this, target);
                damage *= 2;
            }
            CurrentEP = GameMath.Clamp(CurrentEP - 10, 0, EP);
            RaiseOnAttack(target);
            target.TakeDamage(this, damage);
        }
        public void UseSkill(Character target, ISkill skill)
        {
            if (skill.CurrentCooldown <= 0 && CurrentEP >= skill.Cost)
            {
                double damage = (PhysicDamage + MagicDamage) * skill.DamageBonus;
                CurrentEP -= skill.Cost;
                skill.CurrentCooldown = skill.Cooldown;
                OnSkillUsed?.Invoke(this, target, skill);
                target.TakeDamage(this, damage);
            }
        }

        public override void LevelUp()
        {
            Level++;
            HP += 20;
            PhysicDamage += 2;
            MagicDamage += 1;
            EP += 10;
            RestoreFull();
            RaiseOnLevelUp();
        }

        public override void RegenerateEnergy()
        {
            CurrentEP = GameMath.Clamp(CurrentEP + 15, 0, EP);
        }
    }
}