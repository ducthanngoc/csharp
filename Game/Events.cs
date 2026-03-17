using Game.Interfaces;
using Game.Models;
using System;
using System.IO;
using System.Text;

namespace Game.Events
{
    public class StartBattleEvent
    {
        public Character Player { get; }
        public Character Enemy { get; }

        public bool AI1 { get; }
        public bool AI2 { get; }

        public StartBattleEvent(Character p, Character e, bool ai1, bool ai2)
        {
            Player = p;
            Enemy = e;
            AI1 = ai1;
            AI2 = ai2;
        }
    }

    public class EndBattleEvent { }

    public class GameOverEvent { }
    public class BattleLogger
    {
        private StringBuilder logBuilder = new StringBuilder();

        public void Subscribe(Character c)
        {
            if (c == null) return;

            c.OnAttack += OnAttack;
            c.OnDamageTaken += OnDamageTaken;
            c.OnDeath += OnDeath;
            c.OnLevelUp += OnLevelUp;
            if (c is ISkillUser iskilluser) iskilluser.OnSkillUsed += OnSkillUsed;
            if (c is Warrior w) w.OnDefend += OnDefend;
            if (c is Mage m) m.OnHeal += OnHeal;
        }

        public void StartLog()
        {
            logBuilder.Clear();
            logBuilder.AppendLine("========== BATTLE START ==========");
        }

        public void OnTurnStart(int turn)
        {
            logBuilder.AppendLine($"\n[TURN {turn}]");
        }
        private void OnAttack(Character attacker, Character target)
        {
            string msg = $"[ATTACK] {attacker.Name} {attacker.AttackType} {target.Name}. (Current {attacker.EnergyType}: {attacker.CurrentEP})";
            logBuilder.AppendLine(msg);
            Console.WriteLine(msg);
        }

        private void OnDamageTaken(Character defender, Character attacker, double damage)
        {
            string msg = $"[DAMAGE] {defender.Name} received {damage} damage. Current HP: {defender.CurrentHP}";
            logBuilder.AppendLine(msg);
            Console.WriteLine(msg);
        }

        private void OnSkillUsed(Character caster, Character target, ISkill skill)
        {
            string msg = $"[SKILL] {caster.Name} used {skill.SkillName} on {target.Name}! Spent {skill.Cost} {caster.EnergyType}! Current {caster.EnergyType}:{caster.CurrentEP}";
            logBuilder.AppendLine(msg);
            Console.WriteLine(msg);
        }

        private void OnDefend(Character defender)
        {
            string msg = $"[DEFEND] {defender.Name} used defend!";
            logBuilder.AppendLine(msg);
            Console.WriteLine(msg);
        }

        private void OnHeal(Character healer, Character target, double amount)
        {
            string msg = $"[HEAL] {healer.Name} used healing \n {target.Name} was healed {amount} HP!" ;
            logBuilder.AppendLine(msg);
            Console.WriteLine(msg);
        }

        private void OnDeath(Character c)
        {
            string msg = "[DEATH] " + c.Name + " has died!";
            logBuilder.AppendLine(msg);
            Console.WriteLine(msg);
        }

        private void OnLevelUp(Character c)
        {
            logBuilder.AppendLine("[LEVEL UP] " + c.Name + " đã lên cấp " + c.Level + "!");
        }

        public void EndLog()
        {
            logBuilder.AppendLine("\n========== END ==========");
            try
            {
                File.AppendAllText("battle_log.txt", logBuilder.ToString());
                Console.WriteLine("\nBattle's history was saved in 'battle_log.txt'");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error file log saving: " + ex.Message);
            }
        }
    }
}