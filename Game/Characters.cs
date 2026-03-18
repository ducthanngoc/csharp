using Game.Interfaces;
using Game.Math;
using System;
using System.Threading;
namespace Game.Models
{
    public static class LongIdGenerator
    {
        private static long _currentId = 0;
        public static long NextId()
        {
            return Interlocked.Increment(ref _currentId);
        }
    }

    public abstract class Character:CharacterStats
    {
        public event Action<Character, Character> OnAttack;
        public event Action<Character, Character, double> OnDamageTaken;
        public event Action<Character> OnDeath;
        public event Action<Character> OnLevelUp;
        public event Action<Character> OnEvade;
        protected Character(string name)
        {
            ID = LongIdGenerator.NextId();
            Name = name;
            Level = 1;
        }
        public abstract void ShowInfo();
        public abstract void Attack(Character target);
        public abstract void LevelUp();
        public abstract void RegenerateEnergy();

        protected void RaiseOnAttack(Character target) => OnAttack?.Invoke(this, target);
        protected void RaiseOnLevelUp() => OnLevelUp?.Invoke(this);

        public virtual void TakeDamage(Character attacker, double damage)
        {
            if (attacker is IDefend defender) defender.IsDefend = false;
            if (Evade.NextDouble() < EvasionRate) OnEvade?.Invoke(this);
            else
            {
                CurrentHP = GameMath.Clamp(CurrentHP - damage, 0, HP);

                OnDamageTaken?.Invoke(this, attacker, damage);

                if (IsDead())
                {
                    CurrentHP = 0;
                    OnDeath?.Invoke(this);
                }
            }

        }

        public bool IsDead() => CurrentHP <= 0;

        public void RestoreFull()
        {
            CurrentHP = HP;
            CurrentEP = EP;
            if (this is ISkillUser skillUser)
            {
                foreach (var skill in skillUser.Skills)
                {
                    skill.CurrentCooldown = 0;
                }
            }
        }
    }
}