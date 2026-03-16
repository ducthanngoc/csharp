using Game.Interfaces;
using Game.Math;
using Game.Models;
using System;
using System.Collections.Generic;
using Game.Events;
namespace Game.Battle
{
    public class BattleSystem
    {
        Random rand = new Random();
        public static event Action<int> OnTurnStart;

        public static event Action OnTurnEnd;
        public class BattleAction
        {
            public string Name { get; set; }

            public Action Execute { get; set; }

            public BattleAction(string name, Action execute)
            {
                Name = name;
                Execute = execute;
            }
        }
        public List<BattleAction> GetAvailableActions(Character actor, Character target)
        {
            List<BattleAction> actions = new List<BattleAction>();

            actions.Add(new BattleAction(
                "Attack",
                () => actor.Attack(target)
            ));

            if (actor is IDefend defender)
            {
                actions.Add(new BattleAction(
                    "Defend",
                    () => defender.Defend()
                ));
            }
            if (actor is IHealable healer)
            {
                actions.Add(new BattleAction(
                    "Heal",
                    () => healer.Heal(actor,30)
                ));
            }

            if (actor is ISkillUser skillUser)
            {
                foreach (var skill in skillUser.Skills)
                {
                    if (skill.CurrentCooldown <= 0 &&
                        actor.CurrentEP >= skill.Cost)
                    {
                        actions.Add(new BattleAction(
                            skill.SkillName,
                            () => skillUser.UseSkill(target, skill)
                        ));
                    }
                }
            }

            return actions;
        }
        void ExecuteTurn(Character actor, Character target, bool isAI)
        {
            if (actor is IDefend defender && defender.IsDefend) defender.IsDefend = false;
            var actions = GetAvailableActions(actor, target);
            Console.WriteLine($"\n{actor.Name} Turn");
            int choice;

            if (isAI)
            {
                choice = rand.Next(actions.Count);
            }
            else
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {actions[i].Name}");
                }
                Console.Write("Choose action:\n");
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    int number = key.KeyChar - '0';

                    if (number >= 1 && number <= actions.Count)
                    {
                        choice = number - 1;
                        break;
                    }
                }
            }

            actions[choice].Execute();
        }
        void EndTurn(Character c1, Character c2)
        {
            c1.RegenerateEnergy();
            c2.RegenerateEnergy();
            foreach (ISkill skills in c1.Skills)
            {
                skills.CurrentCooldown = GameMath.Clamp(skills.CurrentCooldown - 1, 0, skills.Cooldown);
            }
            foreach (ISkill skills in c2.Skills)
            {
                skills.CurrentCooldown = GameMath.Clamp(skills.CurrentCooldown - 1, 0, skills.Cooldown);
            }
            OnTurnEnd?.Invoke();
        }
        public void StartBattle(Character c1, Character c2,bool AI1, bool AI2)
        {
            BattleLogger.StartLog();
            int turn = 1;
            c1.CurrentHP = c1.HP;
            c2.CurrentHP = c2.HP;
            c1.CurrentEP = c1.EP;
            c2.CurrentEP = c2.EP;
            if (c1 is IDefend d1) d1.IsDefend = false;
            if (c2 is IDefend d2) d2.IsDefend = false;
            while (c1.CurrentHP > 0 && c2.CurrentHP > 0)
            {
                Console.WriteLine($"Turn{turn}");
                OnTurnStart?.Invoke(turn);
                
                ExecuteTurn(c1, c2, AI1);

                if (c2.CurrentHP <= 0)
                    break;

                ExecuteTurn(c2, c1, AI2);
                EndTurn(c1, c2);
                turn++; 
            }

            Console.WriteLine("\n===== RESULT =====");

            if (c1.CurrentHP > 0)
            {
                Console.WriteLine($"{c1.Name} Wins");
                if (c1.Level <= c2.Level) c1.LevelUp();
            }
            else
            {
                if (c2.Level <= c1.Level) c2.LevelUp();
                Console.WriteLine($"{c2.Name} Wins");
            }

            BattleLogger.EndLog();
        }
    }
}