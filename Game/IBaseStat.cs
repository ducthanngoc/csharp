using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Interfaces
{
    public interface IBaseStat
    {
        double HP { get; set; }
        double CurrentHP { get; set; }
        double EP { get; set; }
        double CurrentEP { get; set; }
        double AttackDamage { get; set; }
        int Level { get; set; }
    }
}
