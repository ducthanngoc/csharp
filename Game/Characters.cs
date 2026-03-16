using Game.Delegates;
using Game.Interfaces;
using Game.Math;
using Game.Skills;
using System;
using System.Collections.Generic;
namespace Game.Models
{
    public abstract class Character:IBaseStat, ISkillUser
    {
        private static int _nextID = 1;
        public int ID { get; protected set; }
        public string Name { get; set; }
        public double HP { get; set; }
        public double CurrentHP { get; set; }
        public double EP { get; set; }
        public double CurrentEP { get; set; }
        public double AttackDamage { get; set; }
        public int Level { get; set; }
        public string Class { get; protected set; }
        public List<ISkill> Skills { get; set; } = new List<ISkill>();
        public static event Action<Character, Character> OnAttack;

        public static event Action<Character, double> OnDamageTaken;

        public static event Action<Character> OnDeath;

        public static event Action<Character> OnLevelUp;

        public static event Action<Character, Character, ISkill> OnSkillUsed;
        public Character(string name, double hp, double atk)
        {
            ID = _nextID++;
            Name = name;
            HP = hp;
            CurrentHP = hp;
            AttackDamage = atk;
            Level = 1;
        }
        public abstract void ShowInfo();
        public abstract void Attack(Character target);
        protected void RaiseOnAttack(Character target)
        {
            OnAttack?.Invoke(this, target);
        }
        public virtual void UseSkill(Character target, ISkill skill)
        {
            if (skill.CurrentCooldown <= 0 && CurrentEP >= skill.Cost)
            {
                double damage = AttackDamage * skill.DamageBonus;
                CurrentEP -= skill.Cost;
                skill.CurrentCooldown = skill.Cooldown;
                Logger.Log($"[SKILL] {Name} spent {skill.Cost} SP using {skill.SkillName} on {target.Name}! Current SP is {CurrentEP}!\n");
                RaiseOnSkillUsed(this, target, skill);
                target.TakeDamage(this, damage);
            }
        }
        protected void RaiseOnSkillUsed(Character caster, Character target, ISkill skill)
        {
            OnSkillUsed?.Invoke(this, target, skill);
        }
        public virtual void TakeDamage(Character attacker, double damage)
        {
            if (attacker is IShooter i1)
            {
                bool crit = i1.rand.Next(100) < 20;
                if (crit)
                {
                    damage *= 2;
                    Logger.Log($"[Crit]CRITICAL HIT!\n");
                }
            }
            if (this is IDefend defender && defender.IsDefend)
            {
                Logger.Log($"{Name} defended the attack!\n");
                damage *= 0.5;
                defender.IsDefend = false;
            }
            CurrentHP = GameMath.Clamp(CurrentHP - damage, 0, HP);
            Logger.Log($"[DAMAGE]{Name} received {damage} damage! Current HP is {CurrentHP}!\n");
            RaiseOnDamageTaken(this, damage);
            if (IsDead())
            {
                CurrentHP = 0;
                Logger.Log($"[DEATH] {Name} has died!");
                RaiseOnDeath(this);
            }
        }
        protected void RaiseOnDamageTaken(Character target, double damage)
        {
            OnDamageTaken?.Invoke(target, damage);
        }
        public bool IsDead()
        {
            return CurrentHP <= 0;
        }
        protected void RaiseOnDeath(Character c)
        {
            OnDeath?.Invoke(c);
        }
        public abstract void LevelUp();
        protected void RaiseOnLevelUp(Character c)
        {
            OnLevelUp?.Invoke(c);
        }
        public abstract void RegenerateEnergy();
    }
    public class Warrior : Character,IDefend
    {
        public static event Action<Character> OnDefend;
        public bool IsDefend { get; set; } = false;
        public Warrior(string name)
            : base(name, 300, 20)
        {
            Class = "Warrior";
            EP = 100;
            CurrentEP = 100;
            Skills.Add(new PowerSlash());
        }
        public override void ShowInfo()
        {
            Console.WriteLine($"{Name} | Class: {Class} | HP: {HP} | ATK: {AttackDamage} | SP: {EP} | LV: {Level}");
        }
        public override void Attack(Character target)
        {
            double damage = AttackDamage;
            CurrentEP = GameMath.Clamp(CurrentEP - 10, 0, EP);
            Logger.Log($"[ATTACK]{Name} slashes {target.Name}! Current SP is {CurrentEP}!\n");
            RaiseOnAttack(target);
            target.TakeDamage(this, damage);
        }

        public void Defend()
        {
            CurrentEP = GameMath.Clamp(CurrentEP - 20, 0, EP);
            IsDefend = true;
            Logger.Log($"[DEFEND]{Name} defended! Current SP is {CurrentEP}!\n");
            OnDefend?.Invoke(this);
        }
        public override void TakeDamage(Character attacker, double damage)
        {
            base.TakeDamage(attacker, damage*0.9);
        }
        public override void LevelUp()
        {
            Level++;
            HP += 30;
            AttackDamage += 2;
            EP += 10;
            Console.WriteLine($"[LEVEL UP] {Name} is now level {Level}");
            ShowInfo();
            RaiseOnLevelUp(this);
        }
        public override void RegenerateEnergy()
        {
            CurrentEP = GameMath.Clamp(CurrentEP + 20, 0, EP);
        }
    }
    public class Archer : Character, IShooter
    {
        public Random rand { get; set; } = new Random();
        public Archer(string name)
            : base(name, 200, 30)
        {
            Class = "Archer";
            EP = 100;
            CurrentEP = 100;
            Skills.Add(new FireArrow());
        }
        public override void ShowInfo()
        {
            Console.WriteLine($"{Name} | Class: {Class} | HP: {HP} | ATK: {AttackDamage} | SP: {EP} | LV: {Level}");
        }
        public override void Attack(Character target)
        {
            double damage = AttackDamage;
            CurrentEP = GameMath.Clamp(CurrentEP - 10, 0, EP);
            Logger.Log($"[ATTACK]{Name} shoots {target.Name}! Current SP is {CurrentEP}!\n");
            RaiseOnAttack(target);
            target.TakeDamage(this, damage);
        }
        public override void LevelUp()
        {
            Level++;
            HP += 20;
            AttackDamage += 3;
            EP += 10;
            Console.WriteLine($"[LEVEL UP] {Name} is now level {Level}");
            ShowInfo();
            RaiseOnLevelUp(this);
        }
        public override void RegenerateEnergy()
        {
            CurrentEP = GameMath.Clamp(CurrentEP + 20, 0, EP);
        }
    }
    public class Mage : Character, IHealable
    {
        public double MagicDamage;
        public static event Action<Character, Character, double> OnHeal;
        public Mage(string name)
            : base(name, 150, 40)
        {
            Class = "Mage";
            EP = 200;
            CurrentEP = 200;
            MagicDamage = 20;
            Skills.Add(new FireBall());
        }
        public override void ShowInfo()
        {
            Console.WriteLine($"{Name} | Class: {Class} | HP: {HP} | ATK: {AttackDamage} | MP: {EP} | LV: {Level}");
        }
        public override void Attack(Character target)
        {
            double damage = AttackDamage+MagicDamage;
            CurrentEP = GameMath.Clamp(CurrentEP - 20, 0, EP);
            Logger.Log($"[ATTACK]{Name} casts ManaBall at {target.Name}! Current MP is {CurrentEP}!\n");
            RaiseOnAttack(target);
            target.TakeDamage(this, damage);
        }
        public override void UseSkill(Character target, ISkill skill)
        {
            if (skill.CurrentCooldown <= 0 && CurrentEP >= skill.Cost)
            {
                double damage = AttackDamage +MagicDamage * skill.DamageBonus;
                CurrentEP -= skill.Cost;
                skill.CurrentCooldown = skill.Cooldown;
                Logger.Log($"[SKILL] {Name} spent {skill.Cost} MP using {skill.SkillName} on {target.Name}! Current MP is {CurrentEP}!");
                RaiseOnSkillUsed(this, target, skill);
                target.TakeDamage(this, damage);
            }
        }
        public void Heal(Character target, double amount)
        {
            Logger.Log($"[HEAL]{Name} used healing! Current MP is {CurrentEP}!\n");
            target.CurrentHP = GameMath.Clamp(target.CurrentHP + amount, 0, HP);
            CurrentEP = GameMath.Clamp(CurrentEP - 30, 0, EP);
            Logger.Log($"{target.Name} is healed {amount} HP! {target.Name}'s current HP is {target.CurrentHP}!\n");
            OnHeal?.Invoke(this, target, amount);
        }
        public override void LevelUp()
        {
            Level++;
            HP += 15;
            AttackDamage += 4;
            EP += 20;
            Console.WriteLine($"[LEVEL UP] {Name} is now level {Level}");
            ShowInfo();
            RaiseOnLevelUp(this);
        }
        public override void RegenerateEnergy()
        {
            CurrentEP = GameMath.Clamp(CurrentEP + 30, 0, EP);
        }
    }
}