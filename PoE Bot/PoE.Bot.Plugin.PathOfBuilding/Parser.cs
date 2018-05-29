using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Ionic.Zlib;
using PoE.Bot.Plugin.PathOfBuilding.Models;

namespace PoE.Bot.Plugin.PathOfBuilding
{
    public class Parser
    {
        public Parser() { }

        public Character ParseCode(string base64)
        {
            var xml = XDocument.Parse(FromBase64ToXml(base64));
            xml = XDocument.Parse(xml.ToString().Replace("Spec:", ""));
            
            var summary = new Summary(GetPlayerStats(xml));
            var minionSummary = new MinionSummary(GetMinionStats(xml));
            var skills = GetCharacterSkills(xml);
            var itemSlots = GetItemSlots(xml);
            var items = GetItems(xml);

            var build = xml.Root.Element("Build");
            var level = int.Parse(build.Attribute("level").Value);
            var className = build.Attribute("className").Value;
            var ascendancy = build.Attribute("ascendClassName").Value;
            var tree = GetCharacterTree(xml);
            var auras = GetAurasCount(xml);
            var curses = GetCursesCount(xml);
            var cnf = GetConfig(xml);

            return new Character(level, className, ascendancy, summary, minionSummary, skills, tree, auras, curses, cnf, itemSlots, items);
        }

        private static CharacterSkills GetCharacterSkills(XDocument doc)
        {
            var mainGroup = int.Parse(doc.Root.Element("Build").Attribute("mainSocketGroup").Value) - 1;

            var skills = doc.Root.Element("Skills")
                .Elements("Skill")
                .Select((c, i) =>
                    new SkillGroup(
                        GetGemsFromSkill(c), c.Attribute("slot")?.Value,
                        bool.Parse(c.Attribute("enabled").Value), i == mainGroup))
                .ToList();

            return new CharacterSkills(skills, mainGroup);
        }

        private static IEnumerable<Gem> GetGemsFromSkill(XElement skillElement)
        {
            return skillElement.Elements("Gem")
                .Select(c => {
                    var level = int.Parse(c.Attribute("level").Value);
                    var quality = int.Parse(c.Attribute("quality").Value);
                    var skillId = c.Attribute("skillId").Value;
                    var name = c.Attribute("nameSpec").Value;
                    var enabled = bool.Parse(c.Attribute("enabled").Value);
                    return new Gem(skillId, name, level, quality, enabled);
                });
        }

        private static Dictionary<string, string> GetPlayerStats(XDocument doc)
        {
            return doc.Root
                .Element("Build")
                .Elements("PlayerStat")
                .Select(c => new StatObject(c))
                .Where(c => c.IsValid)
                .GroupBy(c => c.Name, c => c.Value)
                .ToDictionary(c => c.Key, c => c.First());
        }

        private static Dictionary<string, string> GetMinionStats(XDocument doc)
        {
            return doc.Root
                .Element("Build")
                .Elements("MinionStat")
                .Select(c => new StatObject(c))
                .Where(c => c.IsValid)
                .GroupBy(c => c.Name, c => c.Value)
                .ToDictionary(c => c.Key, c => c.First());
        }

        private static string GetCharacterTree(XDocument doc)
        {
            var tree = doc.Root
                .Element("Tree")
                .Element("Spec")
                .Element("URL")
                .Value;

            return tree;
        }

        private static int GetAurasCount(XDocument doc)
        {
            int aurasCount = 0;
            string[] aurasList = { "Anger", "Clarity", "Determination", "Discipline", "Grace", "Haste", "Hatred", "Purity of Elements", "Purity of Fire", "Purity of Ice", "Purity of Lightning", "Vitality", "Wrath", "Envy" };
            var skills = doc.Root.Element("Skills").Elements("Skill");
            foreach(var skill in skills)
            {
                if (bool.Parse(skill.Attribute("enabled").Value))
                {
                    var gems = skill.Elements("Gem");
                    foreach(var gem in gems)
                    {
                        if (bool.Parse(gem.Attribute("enabled").Value))
                        {
                            var gemName = gem.Attribute("nameSpec").Value;
                            if (aurasList.Contains(gemName))
                                aurasCount += 1;
                        }
                    }
                }
            }
            return aurasCount;
        }

        private static int GetCursesCount(XDocument doc)
        {
            int curseCount = 0;
            string[] curseList = { "Poacher's Mark", "Projectile Weakness", "Temporal Chains", "Assasin's Mark", "Assassin's Mark", "Conductivity", "Despair", "Elemental Weakness", "Enfeeble", "Flammability", "Frostbite", "Punishment", "Vulnerability", "Warlord's Mark" };
            var skills = doc.Root.Element("Skills").Elements("Skill");
            foreach (var skill in skills)
            {
                if (bool.Parse(skill.Attribute("enabled").Value))
                {
                    var gems = skill.Elements("Gem");
                    foreach (var gem in gems)
                    {
                        if (bool.Parse(gem.Attribute("enabled").Value))
                        {
                            var gemName = gem.Attribute("nameSpec").Value;
                            if (curseList.Contains(gemName))
                                curseCount += 1;
                        }
                    }
                }
            }
            return curseCount;
        }

        private static IEnumerable<ItemSlots> GetItemSlots(XDocument doc)
        {
            return doc.Root
                .Element("Items")
                .Elements("Slot")
                .Select(c => {
                    var name = c.Attribute("name").Value;
                    var id = int.Parse(c.Attribute("itemId").Value);
                    return new ItemSlots(name, id);
                });
        }

        private static IEnumerable<Items> GetItems(XDocument doc)
        {
            return doc.Root
                .Element("Items")
                .Elements("Item")
                .Select(c=> {
                    var id = int.Parse(c.Attribute("id").Value);
                    var content = c.ToString();
                    return new Items(id, content);
                });
        }

        private static string GetConfig(XDocument doc)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<PathOfBuilding>");
            sb.Append("<Config>");

            foreach (var conf in doc.Root.Element("Config").Elements("Input"))
            {
                sb.Append("<Input ");
                foreach (var attr in conf.Attributes())
                    sb.Append($"{attr.Name}=\"{attr.Value}\" ");
                sb.Append("/>");
            }

            sb.Append("</Config>");
            sb.Append("</PathOfBuilding>");

            return sb.ToString();
        }

        private static string FromBase64ToXml(string base64)
        {
            var dec = Convert.FromBase64String(base64.Replace("-", "+").Replace("_", "/"));
            using (var input = new MemoryStream(dec))
            using (var deflate = new ZlibStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        private class StatObject
        {
            public StatObject(XElement element)
            {
                var attributes = element.Attributes().ToList();
                var nameAttribute = attributes.FirstOrDefault(c => c.Name == "stat");
                var valueAttribute = attributes.FirstOrDefault(c => c.Name == "value");
                Name = nameAttribute?.Value;
                Value = valueAttribute?.Value;
            }

            public string Name { get; }
            public string Value { get; }
            public bool IsValid => Name != null && Value != null;
        }
    }
}
