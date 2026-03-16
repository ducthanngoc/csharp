namespace Game.Interfaces
{
    public interface ISkill
    {
        string SkillName { get;}
        int LevelRequirement { get;}
        double CurrentCooldown { get; set; }
        double Cooldown { get; }
        double DamageBonus { get; }
        double Cost { get; }
    }
}