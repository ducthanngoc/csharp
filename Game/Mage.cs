using Game.Interfaces;
using Game.Math;
using Game.Skills;
using System;
using System.Collections.Generic;
namespace Game.Models
{
    public class Mage : Character, IHealable, ISkillUser
    {
        public event Action<Character, Character, double> OnHeal;
        public event Action<Character, Character, ISkill> OnSkillUsed;
        public List<ISkill> Skills { get; set; } = new List<ISkill>();
        public Mage(string name) : base(name)
        {
            HP = 150;
            CurrentHP = 150;
            EP = 200;
            CurrentEP = 200;
            PhysicDamage = 10;
            MagicDamage = 30;
            Class = "Mage";
            AttackType = "casts ManaBall at";
            EnergyType = "MP";
            EvasionRate = 0.05f;
            Skills.Add(new FireBall());
        }
        public override void ShowInfo()
        {
            Console.WriteLine($"{Name} | Class: {Class} | HP: {CurrentHP}/{HP} | PhysicDamage: {PhysicDamage} | MagicDamage: {MagicDamage} | {EnergyType}: {CurrentEP}/{EP} | LV: {Level}");
        }

        public override void Attack(Character target)
        {
            double damage = PhysicDamage + MagicDamage;
            CurrentEP = GameMath.Clamp(CurrentEP - 20, 0, EP);
            RaiseOnAttack(target);
            target.TakeDamage(this, damage);
        }
        public void UseSkill(Character target, ISkill skill)
        {
            if (skill.CurrentCooldown <= 0 && CurrentEP >= skill.Cost)
            {
                double damage = PhysicDamage + MagicDamage * skill.DamageBonus;
                CurrentEP -= skill.Cost;
                skill.CurrentCooldown = skill.Cooldown;
                OnSkillUsed?.Invoke(this, target, skill);
                target.TakeDamage(this, damage);
            }
        }

        public void Heal(Character target, double amount)
        {
            if (CurrentEP >= 30)
            {
                target.CurrentHP = GameMath.Clamp(target.CurrentHP + amount, 0, target.HP);
                CurrentEP -= 30;
                OnHeal?.Invoke(this, target, amount);
            }
        }

        public override void LevelUp()
        {
            Level++;
            HP += 15;
            PhysicDamage += 1;
            MagicDamage += 3;
            EP += 20;
            RestoreFull();
            RaiseOnLevelUp();
        }

        public override void RegenerateEnergy()
        {
            CurrentEP = GameMath.Clamp(CurrentEP + 25, 0, EP);
        }
    }
}