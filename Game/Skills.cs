using Game.Interfaces;
namespace Game.Skills
{
    public class FireBall : ISkill
    {
        public string SkillName => "FireBall";
        public int LevelRequirement => 2;
        public double CurrentCooldown { get; set; } = 0;
        public double Cooldown => 3;
        public double DamageBonus => 3;
        public double Cost => 60;
    }
    public class PowerSlash : ISkill
    {
        public string SkillName => "PowerSlash";
        public int LevelRequirement => 2;
        public double CurrentCooldown { get; set; } = 0;
        public double Cooldown => 2;
        public double DamageBonus => 1.5;
        public double Cost => 20;
    }
    public class FireArrow : ISkill
    {
        public string SkillName => "FireArrow";
        public int LevelRequirement => 2;
        public double CurrentCooldown { get; set; } = 0;
        public double Cooldown => 3;
        public double DamageBonus => 1.8;
        public double Cost => 30;
    }
}
