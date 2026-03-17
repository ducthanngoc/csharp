using Game.Core;
using Game.Models;
using System;
using System.Collections.Generic;
namespace Game.Interfaces
{
    public interface IDefend
    {
        event Action<Character> OnDefend;
        bool IsDefend { get; set; }
        void Defend();
    }
    public interface IHealable
    {
        event Action<Character, Character, double> OnHeal;
        void Heal(Character target, double amount);
    }
    public interface IShooter
    {
        event Action<Character, Character> OnCrit;
        Random rand { get; set; }    }
    public interface ISkill
    {
        string SkillName { get; }
        int LevelRequirement { get; }
        double CurrentCooldown { get; set; }
        double Cooldown { get; }
        double DamageBonus { get; }
        double Cost { get; }
    }
    public interface ISkillUser
    {
        event Action<Character, Character, ISkill> OnSkillUsed;
        List<ISkill> Skills { get; set; }
        void UseSkill(Character target, ISkill skill);
    }
}
