using Game.Battle;
using Game.Interfaces;
using Game.Models;
using System.IO;
using System.Text;
namespace Game.Events
{
    public static class BattleLogger
    {
        private static StringBuilder logBuilder = new StringBuilder();

        public static void Subscribe()
        {
            Character.OnAttack += OnAttack;
            Character.OnDamageTaken += OnDamageTaken;
            Character.OnDeath += OnDeath;
            Character.OnLevelUp += OnLevelUp;
            Character.OnSkillUsed += OnSkillUsed;
            BattleSystem.OnTurnStart += OnTurnStart;
            Warrior.OnDefend += OnDefend;
            Mage.OnHeal += OnHeal;
        }
        public static void StartLog()
        {
            logBuilder.Clear();
            logBuilder.AppendLine("===== BATTLE START =====");
        }
        public static void EndLog()
        {
            logBuilder.AppendLine("===== BATTLE END =====");

            File.AppendAllText("battle_log.txt", logBuilder.ToString());
        }
        public static void OnTurnStart(int turn)
        {
            logBuilder.AppendLine($"Turn {turn}\n");
        }
        private static void OnAttack(Character attacker, Character target)
        {
            string type,ep;

            if (attacker is Mage)
            {
                type = "casts ManaBall at";
                ep = "MP";
            }
            else if (attacker is Warrior)
            {
                type = "slashes";
                ep = "SP";
            }
            else if (attacker is Archer)
            {
                type = "shoots an arrow at";
                ep = "SP";
            }
            else
            {
                type = "attacks";
                ep = "SP";
            }
            logBuilder.AppendLine($"[ATTACK]{attacker.Name} {type} {target.Name}! Current {ep} is {attacker.CurrentEP}!\n");
        }
        private static void OnDefend(Character defender)
        {
            logBuilder.AppendLine($"[DEFEND]{defender.Name} defended! Current SP is {defender.CurrentEP}!\n");
        }
        private static void OnHeal(Character healer, Character target, double amount)
        {
            logBuilder.AppendLine($"[HEAL]{healer.Name} used healing! Current MP is {healer.CurrentEP}!\n");
            logBuilder.AppendLine($"{target.Name} is healed {amount} HP! {target.Name}'s current HP is {target.CurrentHP}!\n");
        }
        private static void OnDamageTaken(Character target, double damage)
        {
            logBuilder.AppendLine($"[DAMAGE]{target.Name} received {damage} damage! Current HP is {target.CurrentHP}!\n");
        }

        private static void OnDeath(Character c)
        {
            logBuilder.AppendLine($"[DEATH] {c.Name} has died!");
        }

        private static void OnLevelUp(Character c)
        {
            logBuilder.AppendLine($"[LEVEL UP] {c.Name} is now level {c.Level}");
        }

        private static void OnSkillUsed(Character caster, Character target, ISkill skill)
        {
            string ep;
            if (caster is Mage)
                ep = "MP";
            else if (caster is Warrior)
                ep = "SP";
            else if (caster is Archer)
                ep = "SP";
            else
                ep = "SP";
            logBuilder.AppendLine($"[SKILL] {caster.Name} spent {skill.Cost} {ep} using {skill.SkillName} on {target.Name}! Current {ep} is {caster.CurrentEP}!\n");
        }
    }
}
