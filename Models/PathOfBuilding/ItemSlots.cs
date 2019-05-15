namespace PoE.Bot.Models.PathOfBuilding
{
	public class ItemSlots
	{
		public ItemSlots(string name, int itemID)
		{
			ItemID = itemID;
			Name = name;
		}

		public int ItemID { get; private set; }
		public string Name { get; private set; }
	}
}