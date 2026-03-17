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

    public abstract class Character
    {
        public long ID { get; protected set; }
        public string Name { get; set; }
        public double HP { get; protected set; }
        public double CurrentHP { get; set; }
        public double EP { get; set; }
        public double CurrentEP { get; set; }
        public double PhysicDamage { get; set; }
        public double MagicDamage { get; set; }
        public int Level { get; set; }

        public string Class { get; protected set; }
        public string AttackType { get; protected set; }
        public string EnergyType { get; protected set; }
        public event Action<Character, Character> OnAttack;
        public event Action<Character, Character, double> OnDamageTaken;
        public event Action<Character> OnDeath;
        public event Action<Character> OnLevelUp;
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

            CurrentHP = GameMath.Clamp(CurrentHP - damage, 0, HP);

            OnDamageTaken?.Invoke(this, attacker, damage);

            if (IsDead())
            {
                CurrentHP = 0;
                OnDeath?.Invoke(this);
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