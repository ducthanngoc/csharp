using Game.Models;
using System;
using System.Collections.Generic;
namespace Game.Core
{
    public class CharacterFactory
    {
        public Dictionary<int, Func<string, Character>> factory = new Dictionary<int, Func<string, Character>>()
        {
                    {0, name => new Warrior(name)},
                    {1, name => new Archer(name)},
                    {2, name => new Mage(name)}
        };
        public Character CreateCharacter(int index, string name)
        {
            return factory[index](name);
        }
    }
}
