namespace PoE.Bot.Plugin.PathOfBuilding.Models
{
    public class ItemSlots
    {
        public ItemSlots(string name, int itemID)
        {
            Name = name;
            ItemID = itemID;
        }

        public string Name { get; private set; }
        public int ItemID { get; private set; }
    }
}
