namespace PoE.Bot.Helpers.Objects.PathOfBuilding
{
    using System.Collections.Generic;

    public class Character
    {
        public Character(int level, string characterClass, string ascendancy, Summary summary, MinionSummary minionSummary, CharacterSkills skills, string tree, int auraCount, int curseCount
            , string config, IEnumerable<ItemSlots> itemSlots, IEnumerable<Items> items)
        {
            Level = level;
            Class = characterClass;
            Ascendancy = ascendancy;
            Summary = summary;
            MinionSummary = minionSummary;
            Skills = skills;
            Tree = tree;
            AuraCount = auraCount;
            CurseCount = curseCount;
            Config = config;
            ItemSlots = itemSlots;
            Items = items;
        }

        public string Ascendancy { get; }
        public string Class { get; }
        public int Level { get; }

        public Summary Summary { get; }
        public MinionSummary MinionSummary { get; }
        public CharacterSkills Skills { get; }

        public string Tree { get; }
        public int AuraCount { get; }
        public int CurseCount { get; }

        public string Config { get; }

        public IEnumerable<ItemSlots> ItemSlots { get; }
        public IEnumerable<Items> Items { get; }
    }
}
