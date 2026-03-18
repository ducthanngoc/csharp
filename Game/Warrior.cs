using Game.Interfaces;
using Game.Math;
using Game.Skills;
using System;
using System.Collections.Generic;
namespace Game.Models
{
    public class Warrior : Character, IDefend, ISkillUser
    {
        public event Action<Character> OnDefend;
        public event Action<Character, Character, ISkill> OnSkillUsed;
        public bool IsDefend { get; set; }
        public List<ISkill> Skills { get; set; } = new List<ISkill>();

        public Warrior(string name) : base(name)
        {
            HP = 300;
            CurrentHP = 300;
            EP = 100;
            CurrentEP = 100;
            PhysicDamage = 25;
            MagicDamage = 0;
            Class = "Warrior";
            AttackType = "slashes";
            EnergyType = "SP";
            EP = 100;
            CurrentEP = 100;
            EvasionRate = 0.1f;
            Skills.Add(new PowerSlash());
        }
        public override void ShowInfo()
        {
            Console.WriteLine($"{Name} | Class: {Class} | HP: {CurrentHP}/{HP} | PhysicDamage: {PhysicDamage} | MagicDamage: {MagicDamage} | {EnergyType}: {CurrentEP}/{EP} | LV: {Level}");
        }
        public override void Attack(Character target)
        {
            double damage = PhysicDamage;
            CurrentEP = GameMath.Clamp(CurrentEP - 10, 0, EP);
            RaiseOnAttack(target);
            target.TakeDamage(this, damage);
        }

        public void UseSkill(Character target, ISkill skill)
        {
            if (skill.CurrentCooldown <= 0 && CurrentEP >= skill.Cost)
            {
                double damage = PhysicDamage * skill.DamageBonus;
                CurrentEP -= skill.Cost;
                skill.CurrentCooldown = skill.Cooldown;
                OnSkillUsed?.Invoke(this, target, skill);
                target.TakeDamage(this, damage);
            }
        }

        public void Defend()
        {
            CurrentEP = GameMath.Clamp(CurrentEP - 20, 0, EP);
            IsDefend = true;
            OnDefend?.Invoke(this);
        }
        public override void TakeDamage(Character attacker, double damage)
        {
            if (IsDefend)
            {
                damage *= 0.5;
                IsDefend = false;
                base.TakeDamage(attacker, damage);
            }
            else base.TakeDamage(attacker, damage * 0.9);
        }

        public override void LevelUp()
        {
            Level++;
            HP += 30;
            PhysicDamage += 2.5;
            EP += 10;
            RestoreFull();
            RaiseOnLevelUp();
        }

        public override void RegenerateEnergy()
        {
            CurrentEP = GameMath.Clamp(CurrentEP + 10, 0, EP);
        }
    }
}