using Game.Core;
using Game.Events;
using Game.Interfaces;
using Game.Math;
using Game.Models;
using System;
using System.Collections.Generic;

namespace Game.Battle
{
    public class BattleSystem
    {
        private readonly EventBus _eventBus;
        private Random rand = new Random();

        private Character _c1;
        private Character _c2;

        private bool _isActive;
        private int _turn;

        private bool _ai1;
        private bool _ai2;

        public BattleLogger Logger { get; }
        public BattleSystem(EventBus eventBus)
        {
            _eventBus = eventBus;
            Logger = new BattleLogger();
        }

        public void StartBattle(Character c1, Character c2, bool ai1, bool ai2)
        {
            _c1 = c1;
            _c2 = c2;

            _ai1 = ai1;
            _ai2 = ai2;

            _turn = 1;
            _isActive = true;

            _c1.RestoreFull();
            _c2.RestoreFull();
            Logger.StartLog();

            Console.Clear();
            Console.WriteLine("===== BATTLE START =====");
            Console.WriteLine($"{_c1.Name} vs {_c2.Name}");
        }

        public void Update()
        {
            if (!_isActive) return;

            Console.WriteLine($"\n--- TURN {_turn} ---");
            Logger.OnTurnStart(_turn);

            ExecuteTurn(_c1, _c2, _ai1);
            if (CheckEnd()) return;

            ExecuteTurn(_c2, _c1, _ai2);
            if (CheckEnd()) return;

            EndTurnCycle();
            _turn++;

            //if (_ai1 && _ai2)
            //    System.Threading.Thread.Sleep(500);
        }

        private void ExecuteTurn(Character actor, Character target, bool isAI)
        {
            var actions = GetAvailableActions(actor, target);

            if (isAI)
            {
                actions[rand.Next(actions.Count)].Execute();
            }
            else
            {
                Console.WriteLine($"\n{actor.Name} turn:");

                for (int i = 0; i < actions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {actions[i].Name}");
                }

                int choice = GetChoice(actions.Count);
                actions[choice - 1].Execute();
            }
        }

        public List<BattleAction> GetAvailableActions(Character actor, Character target)
        {
            var actions = new List<BattleAction>();

            actions.Add(new BattleAction("ATTACK", () => actor.Attack(target)));

            if (actor is IDefend d)
                actions.Add(new BattleAction("DEFEND", () => d.Defend()));

            if (actor is IHealable h)
                actions.Add(new BattleAction("HEAL", () => h.Heal(actor, 40)));

            if (actor is ISkillUser sku)
            {
                foreach (var skill in sku.Skills)
                {
                    if (skill.CurrentCooldown <= 0 && actor.CurrentEP >= skill.Cost)
                    {
                        actions.Add(new BattleAction(
                            $"SKILL: {skill.SkillName}",
                            () => sku.UseSkill(target, skill)
                        ));
                    }
                }
            }

            return actions;
        }

        private bool CheckEnd()
        {
            if (_c1.IsDead() || _c2.IsDead())
            {
                EndBattle();
                return true;
            }
            return false;
        }

        private void EndBattle()
        {
            _isActive = false;

            Console.WriteLine("\n===== RESULT =====");
            var winner = _c1.CurrentHP > 0 ? _c1 : _c2;
            Console.WriteLine($"{winner.Name} WIN!");

            Logger.EndLog();

            _eventBus.Publish(new EndBattleEvent());
        }

        private void EndTurnCycle()
        {
            _c1.RegenerateEnergy();
            _c2.RegenerateEnergy();

            if (_c1 is ISkillUser s1)
                foreach (var s in s1.Skills)
                    s.CurrentCooldown = GameMath.Clamp(s.CurrentCooldown - 1, 0, s.Cooldown);

            if (_c2 is ISkillUser s2)
                foreach (var s in s2.Skills)
                    s.CurrentCooldown = GameMath.Clamp(s.CurrentCooldown - 1, 0, s.Cooldown);
        }

        private int GetChoice(int max)
        {
            while (true)
            {
                var key = Console.ReadKey(true);

                if (char.IsDigit(key.KeyChar))
                {
                    int val = key.KeyChar - '0';
                    if (val >= 1 && val <= max)
                        return val;
                }
            }
        }

        public class BattleAction
        {
            public string Name;
            public Action Execute;

            public BattleAction(string name, Action execute)
            {
                Name = name;
                Execute = execute;
            }
        }
    }
}