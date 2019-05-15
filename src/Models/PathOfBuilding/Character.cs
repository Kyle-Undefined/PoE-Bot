namespace PoE.Bot.Models.PathOfBuilding
{
	using System.Collections.Generic;

	public class Character
	{
		public Character(int level, string characterClass, string ascendancy, Summary summary, MinionSummary minionSummary, CharacterSkills skills, string tree, int auraCount, int curseCount,
			string config, IEnumerable<ItemSlots> itemSlots, IEnumerable<Items> items)
		{
			Ascendancy = ascendancy;
			AuraCount = auraCount;
			Class = characterClass;
			Config = config;
			CurseCount = curseCount;
			Items = items;
			ItemSlots = itemSlots;
			Level = level;
			MinionSummary = minionSummary;
			Skills = skills;
			Summary = summary;
			Tree = tree;
		}

		public string Ascendancy { get; }
		public int AuraCount { get; }
		public string Class { get; }
		public string Config { get; }
		public int CurseCount { get; }
		public IEnumerable<Items> Items { get; }
		public IEnumerable<ItemSlots> ItemSlots { get; }
		public int Level { get; }

		public MinionSummary MinionSummary { get; }
		public CharacterSkills Skills { get; }
		public Summary Summary { get; }
		public string Tree { get; }
	}
}