namespace PoE.Bot.Objects.PathOfBuilding
{
    public class Gem
    {
        public Gem(string skillId, string name, int level, int quality, bool enabled)
        {
            Enabled = enabled;
            Name = name;
            SkillId = skillId;
            Level = level;
            Quality = quality;
        }

        public bool Enabled { get; }
        public string Name { get; }
        public string SkillId { get; }
        public int Level { get; }
        public int Quality { get; }
    }
}
