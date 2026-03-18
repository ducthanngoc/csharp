using Game.Battle;
using Game.Events;
using Game.Models;
using System;
using System.Collections.Generic;
namespace Game.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Battle,
        Pause,
        GameOver
    }
    public sealed class GameManager
    {
        private static readonly Lazy<GameManager> _instance =
            new Lazy<GameManager>(() => new GameManager());
        public static GameManager Instance => _instance.Value;

        private GameManager()
        {
            InitializeSystems();
            LoadCharacters();
        }

        public EventBus EventBus { get; private set; }
        public BattleSystem BattleSystem { get; private set; }

        private readonly List<Character> templates = new List<Character>();
        private readonly Random rand = new Random();

        public GameState CurrentState { get; private set; }
        private bool _isRunning;

        private void InitializeSystems()
        {
            EventBus = new EventBus();
            BattleSystem = new BattleSystem(EventBus);
        }

        private void LoadCharacters()
        {
            templates.Add(new Warrior("1"));
            templates.Add(new Archer("2"));
            templates.Add(new Mage("3"));
        }

        public void Init()
        {
            RegisterEvents();
            ChangeState(GameState.Menu);
        }

        public void Start()
        {
            _isRunning = true;

            while (_isRunning)
            {
                Update();
                //System.Threading.Thread.Sleep(50);
            }
        }

        private void Update()
        {
            switch (CurrentState)
            {
                case GameState.Menu:
                    UpdateMenu();
                    break;

                case GameState.Playing:
                    UpdatePlaying();
                    break;

                case GameState.Battle:
                    BattleSystem.Update();
                    break;

                case GameState.GameOver:
                    _isRunning = false;
                    break;
            }
        }

        public void ChangeState(GameState newState)
        {
            Console.Clear();
            Console.WriteLine($"State: {CurrentState} -> {newState}");
            CurrentState = newState;
        }

        private void RegisterEvents()
        {
            EventBus.Subscribe<StartBattleEvent>(OnStartBattle);
            EventBus.Subscribe<EndBattleEvent>(OnEndBattle);
        }

        private void OnStartBattle(StartBattleEvent e)
        {
            ChangeState(GameState.Battle);

            BattleSystem.StartBattle(
                e.Player,
                e.Enemy,
                e.AI1,
                e.AI2
            );
        }

        private void OnEndBattle(EndBattleEvent e)
        {
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
            ChangeState(GameState.Playing);
        }

        private void UpdateMenu()
        {
            Console.WriteLine("Press ENTER to start");
            ConsoleKeyInfo key;
            while (true)
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
            ChangeState(GameState.Playing);
        }

        private void UpdatePlaying()
        {
            Console.WriteLine("\nPress B to Battle | Q to Quit");

            ConsoleKeyInfo key;
            while (_isRunning)
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.B)
                {
                    StartBattleFlow();
                    break;
                }
                else if (key.Key == ConsoleKey.Q)
                {
                    _isRunning = false;
                }
            }
        }

        private void StartBattleFlow()
        {
            Console.Clear();
            Console.WriteLine("=== SELECT MODE ===");
            Console.WriteLine("1. Player vs Player");
            Console.WriteLine("2. Player vs AI");
            Console.WriteLine("3. AI vs AI");

            int mode = GetChoice(3);

            Character c1;
            Character c2;

            bool ai1 = false;
            bool ai2 = false;

            switch (mode)
            {
                case 1:
                    Console.WriteLine("\nPlayer 1 choose:");
                    c1 = ChooseCharacter();

                    Console.WriteLine("\nPlayer 2 choose:");
                    c2 = ChooseCharacter();
                    break;

                case 2:
                    Console.WriteLine("\nPlayer choose:");
                    c1 = ChooseCharacter();

                    c2 = GetRandomCharacter();
                    ai2 = true;
                    break;

                case 3:
                    c1 = GetRandomCharacter();
                    c2 = GetRandomCharacter();

                    ai1 = true;
                    ai2 = true;

                    Console.WriteLine($"\nAI Battle: {c1.Name} vs {c2.Name}");
                    break;

                default:
                    return;
            }
            EventBus.Publish(new StartBattleEvent(c1, c2, ai1, ai2));
        }

        private Character ChooseCharacter()
        {
            Console.WriteLine("Choose your character class:");

            for (int i = 0; i < templates.Count; i++)
            {
                templates[i].ShowInfo();
            }

            int choice = GetChoice(templates.Count);
            int classIndex = choice - 1;

            Console.Write($"Enter name for your {templates[classIndex].Class}: ");
            string charName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(charName))
            {
                charName = templates[classIndex].Class;
            }

            return CreateCharacterInstance(classIndex, charName);
        }

        private Character GetRandomCharacter()
        {
            int index = rand.Next(templates.Count);
            Character AI = CreateCharacterInstance(index, "");
            AI.Name = $"AI_{AI.ID}_{AI.Class}";
            return AI;
        }
        private Character CreateCharacterInstance(int index, string name)
        {
            Character newChar = new CharacterFactory().CreateCharacter(index,name);
            BattleSystem.Logger.Subscribe(newChar);

            return newChar;
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
    }
}