namespace PoE.Bot.Models.PathOfBuilding
{
	using System.Collections.Generic;

	public class CharacterSkills
	{
		public CharacterSkills(IReadOnlyList<SkillGroup> skillGroups, int mainSkillIndex)
		{
			MainSkillIndex = mainSkillIndex;
			SkillGroups = skillGroups;
		}

		public SkillGroup MainSkillGroup => SkillGroups[MainSkillIndex];
		public int MainSkillIndex { get; }
		public IReadOnlyList<SkillGroup> SkillGroups { get; }
	}
}