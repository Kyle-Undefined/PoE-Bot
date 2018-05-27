using System.Collections.Generic;

namespace PoE.Bot.Plugin.PathOfBuilding.Models
{
    public class CharacterSkills
    {
        public CharacterSkills(IReadOnlyList<SkillGroup> skillGroups, int mainSkillIndex)
        {
            SkillGroups = skillGroups;
            MainSkillIndex = mainSkillIndex;
        }

        public int MainSkillIndex { get; }
        public IReadOnlyList<SkillGroup> SkillGroups { get; }

        public SkillGroup MainSkillGroup => SkillGroups[MainSkillIndex];
    }
}
