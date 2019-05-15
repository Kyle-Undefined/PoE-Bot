namespace PoE.Bot.Models.PathOfBuilding
{
	using System.Collections.Generic;
	using System.Linq;

	public class SkillGroup
	{
		public SkillGroup(IEnumerable<Gem> gems, string slot, bool isEnabled, bool isSelectedGroup)
		{
			Gems = gems.ToList();
			IsEnabled = isEnabled;
			IsSelectedGroup = isSelectedGroup;
			Slot = slot;
		}

		public IReadOnlyList<Gem> Gems { get; }
		public bool IsEnabled { get; }
		public bool IsSelectedGroup { get; }
		public string Slot { get; }
	}
}