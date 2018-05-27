namespace PoE.Bot.Plugin.PathOfBuilding.Models
{
    public class Items
    {
        public Items(int iD, string content)
        {
            ID = iD;
            Content = content;
        }

        public int ID { get; }
        public string Content { get; }
    }
}
