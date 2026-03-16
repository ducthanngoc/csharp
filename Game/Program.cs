using Game.Battle;
using Game.Delegates;
using Game.Events;
using Game.Models;
using System;
using System.Collections.Generic;
class Program
{
    public static Character CreateCharacter(int choice, string name)
    {
        switch (choice)
        {
            case 1: return new Warrior(name);
            case 2: return new Archer(name);
            case 3: return new Mage(name);
            default: return null;
        }
    }
    static void Main()
    {
        BattleLogger.Subscribe();
        Logger.OnLog += Console.WriteLine;
        Console.WriteLine("Welcome to basic game!");
        Random rand = new Random();
        List<Character> characters = new List<Character>();
        characters.Add(new Warrior("1"));
        characters.Add(new Archer("2"));
        characters.Add(new Mage("3"));
        for (int i = 0; i < characters.Count; i++)
        {
            characters[i].ShowInfo();
        }
        List<Character> computer1 = new List<Character>()
        {
            new Warrior("Thor-PC1"),
            new Archer("Legolas-PC1"),
            new Mage("Merlin-PC1")
        };
        List<Character> computer2 = new List<Character>()
        {
            new Warrior("Thor-PC2"),
            new Archer("Legolas-PC2"),
            new Mage("Merlin-PC2")
        };
        BattleSystem battle = new BattleSystem();
        int mode = 0;
        Console.WriteLine("Choose Battle Mode:");
        Console.WriteLine("1. Player vs Player");
        Console.WriteLine("2. Player vs AI");
        Console.WriteLine("3. AI vs AI");
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            if (key.KeyChar >= '1' && key.KeyChar <= '3')
            {
                mode = key.KeyChar - '0';
                break;
            }
        }
        bool AI1 = false;
        bool AI2 = false;
        Character c1;
        Character c2;
        int choice;
        string name;
        if (mode < 3)
        {
            Console.WriteLine("Choose Character 1:");
            for (int i = 0; i < characters.Count; i++)
            {
                characters[i].ShowInfo();
            }
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.KeyChar >= '1' && key.KeyChar <= '3')
                {
                    choice = key.KeyChar - '0';
                    break;
                }
            }
            Console.WriteLine("Enter name:");
            name = Console.ReadLine();
            c1 = CreateCharacter(choice, name);
            if (mode < 2)
            {
                Console.WriteLine("Choose Character 2:");
                for (int i = 0; i < characters.Count; i++)
                {
                    characters[i].ShowInfo();
                }
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key.KeyChar >= '1' && key.KeyChar <= '3')
                    {
                        choice = key.KeyChar - '0';
                        break;
                    }
                }
                Console.WriteLine("Enter name:");
                name = Console.ReadLine();
                c2 = CreateCharacter(choice, name);
            }
            else
            {
                c2 = computer2[rand.Next(computer2.Count)];
                AI2 = true;
            }
        }
        else
        {
            c1 = computer1[rand.Next(computer1.Count)];
            c2 = computer2[rand.Next(computer2.Count)];
            AI1 = true;
            AI2 = true;
        }
        battle.StartBattle(c1, c2,AI1, AI2);
        while (mode == 2)
        {
            Console.WriteLine("Do you want to continue? (Y/N)");
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                choice = Char.ToUpper(key.KeyChar);
                if (choice == 'Y' || choice == 'N') break;
            }
            if (choice == 'N') break;
            c2 = computer2[rand.Next(computer2.Count)];
            battle.StartBattle(c1, c2, AI1, AI2);
        }
        Console.WriteLine("Press any key to exit");
        Console.ReadKey(true);
    }
}