using Game.Models;
using System;
using System.Collections.Generic;
namespace Game.Interfaces
{
    public interface ISkillUser
    {
        List<ISkill> Skills { get; set; }
        void UseSkill(Character target,ISkill skill);
    }
}
