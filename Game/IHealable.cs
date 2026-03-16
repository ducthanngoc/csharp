using Game.Models;
namespace Game.Interfaces
{
    public interface IHealable
    {
        void Heal(Character target, double amount);
    }
}
