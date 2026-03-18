using System;

namespace Game.Models
{
    public class CharacterStats
    {
        public long ID { get; protected set; }
        public string Name { get; set; }
        public double HP { get; set; }
        public double CurrentHP { get; set; }
        public double EP { get; set; }
        public double CurrentEP { get; set; }
        public double PhysicDamage { get; set; }
        public double MagicDamage { get; set; }
        public int Level { get; set; }
        public string Class { get; protected set; }
        public string AttackType { get; protected set; }
        public string EnergyType { get; protected set; }
        public float EvasionRate { get; set; }
        public float CritRate { get; set; }
        public Random Evade { get;} = new Random();
    }
}
