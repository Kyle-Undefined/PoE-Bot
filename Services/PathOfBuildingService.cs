namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using Ionic.Zlib;
	using Newtonsoft.Json.Linq;
	using PoE.Bot.Attributes;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models.PathOfBuilding;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net.Http;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	public class Config
	{
		public Config(string var, string abbreviation, string category, string value)
		{
			Abbreviation = abbreviation;
			Category = category;
			Value = value;
			Var = var;
		}

		public string Abbreviation { get; }
		public string Category { get; }
		public string Value { get; }
		public string Var { get; }
	}

	public class Parser
	{
		public Character ParseCode(string base64)
		{
			var xml = XDocument.Parse(FromBase64ToXml(base64));
			xml = XDocument.Parse(xml.ToString().Replace("Spec:", string.Empty));

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

		private string FromBase64ToXml(string base64)
		{
			byte[] dec = Convert.FromBase64String(base64.Replace("-", "+").Replace("_", "/"));
			using (var input = new MemoryStream(dec))
			using (var deflate = new ZlibStream(input, CompressionMode.Decompress))
			using (var output = new MemoryStream())
			{
				deflate.CopyTo(output);
				return Encoding.UTF8.GetString(output.ToArray());
			}
		}

		private int GetAurasCount(XDocument doc)
		{
			var aurasCount = 0;
			string[] aurasList = { "Anger", "Clarity", "Determination", "Discipline", "Grace", "Haste", "Hatred", "Purity of Elements", "Purity of Fire", "Purity of Ice", "Purity of Lightning", "Vitality", "Wrath", "Envy" };
			foreach (var skill in doc.Root.Element("Skills").Elements("Skill"))
			{
				if (bool.Parse(skill.Attribute("enabled").Value))
				{
					foreach (var gem in skill.Elements("Gem"))
					{
						if (bool.Parse(gem.Attribute("enabled").Value))
						{
							string gemName = gem.Attribute("nameSpec").Value;
							if (aurasList.Contains(gemName))
								aurasCount++;
						}
					}
				}
			}
			return aurasCount;
		}

		private CharacterSkills GetCharacterSkills(XDocument doc)
		{
			var mainGroup = int.Parse(doc.Root.Element("Build").Attribute("mainSocketGroup").Value) - 1;
			var skills = doc.Root.Element("Skills").Elements("Skill")
				.Select((x, y) => new SkillGroup(GetGemsFromSkill(x), x.Attribute("slot")?.Value, bool.Parse(x.Attribute("enabled").Value), y == mainGroup)).ToList();

			return new CharacterSkills(skills, mainGroup);
		}

		private string GetCharacterTree(XDocument doc) => doc.Root.Element("Tree").Element("Spec").Element("URL").Value;

		private string GetConfig(XDocument doc)
		{
			var sb = new StringBuilder();

			sb.Append("<PathOfBuilding>");
			sb.Append("<Config>");

			foreach (var conf in doc.Root.Element("Config").Elements("Input"))
			{
				sb.Append("<Input ");
				foreach (var attr in conf.Attributes())
					sb.Append(attr.Name).Append("=\"").Append(attr.Value).Append("\" ");
				sb.Append("/>");
			}

			sb.Append("</Config>");
			sb.Append("</PathOfBuilding>");

			return sb.ToString();
		}

		private int GetCursesCount(XDocument doc)
		{
			var curseCount = 0;
			string[] curseList = { "Poacher's Mark", "Projectile Weakness", "Temporal Chains", "Assasin's Mark", "Assassin's Mark", "Conductivity", "Despair", "Elemental Weakness", "Enfeeble", "Flammability", "Frostbite", "Punishment", "Vulnerability", "Warlord's Mark" };
			foreach (var skill in doc.Root.Element("Skills").Elements("Skill"))
			{
				if (bool.Parse(skill.Attribute("enabled").Value))
				{
					foreach (var gem in skill.Elements("Gem"))
					{
						if (bool.Parse(gem.Attribute("enabled").Value))
						{
							var gemName = gem.Attribute("nameSpec").Value;
							if (curseList.Contains(gemName))
								curseCount++;
						}
					}
				}
			}
			return curseCount;
		}

		private IEnumerable<Gem> GetGemsFromSkill(XElement skillElement) => skillElement.Elements("Gem").Select(c =>
		{
			int level = int.Parse(c.Attribute("level").Value);
			int quality = int.Parse(c.Attribute("quality").Value);
			string skillId = c.Attribute("skillId").Value;
			string name = c.Attribute("nameSpec").Value;
			bool enabled = bool.Parse(c.Attribute("enabled").Value);
			return new Gem(skillId, name, level, quality, enabled);
		});

		private IEnumerable<Items> GetItems(XDocument doc) => doc.Root.Element("Items").Elements("Item").Select(c =>
		{
			var id = int.Parse(c.Attribute("id").Value);
			var content = c.ToString();
			return new Items(id, content);
		});

		private IEnumerable<ItemSlots> GetItemSlots(XDocument doc) => doc.Root.Element("Items").Elements("Slot").Select(c =>
		{
			var name = c.Attribute("name").Value;
			var id = int.Parse(c.Attribute("itemId").Value);
			return new ItemSlots(name, id);
		});

		private Dictionary<string, string> GetMinionStats(XDocument doc) =>
			doc.Root.Element("Build").Elements("MinionStat").Select(x => new StatObject(x)).Where(x => x.IsValid).GroupBy(x => x.Name, x => x.Value).ToDictionary(x => x.Key, x => x.First());

		private Dictionary<string, string> GetPlayerStats(XDocument doc) =>
			doc.Root.Element("Build").Elements("PlayerStat").Select(x => new StatObject(x)).Where(x => x.IsValid).GroupBy(x => x.Name, x => x.Value).ToDictionary(x => x.Key, x => x.First());

		private class StatObject
		{
			public StatObject(XElement element)
			{
				var attributes = element.Attributes().ToList();
				var nameAttribute = attributes.Find(x => x.Name == "stat");
				var valueAttribute = attributes.Find(x => x.Name == "value");
				Name = nameAttribute?.Value;
				Value = valueAttribute?.Value;
			}

			public bool IsValid => !(Name is null) && !(Value is null);
			public string Name { get; }
			public string Value { get; }
		}
	}

	public class PasteBinFetcher
	{
		private readonly string _endpoint = "https://pastebin.com/raw/";
		private readonly HttpClient _httpClient;

		public PasteBinFetcher(HttpClient httpClient) => _httpClient = httpClient;

		public async Task<string> GetRawCodeAsync(string url)
		{
			if (!url.StartsWith("https://pastebin.com/"))
				throw new ArgumentException("That's not a valid pastebin url", nameof(url));

			var code = url.Split('/').Last();
			return await _httpClient.GetStringAsync(_endpoint + code).ConfigureAwait(false);
		}
	}

	[Service]
	public class PathOfBuildingService
	{
		private readonly HttpClient _httpClient;

		public PathOfBuildingService(HttpClient httpClient) => _httpClient = httpClient;

		public async Task<Embed> BuildPathOfBuildingEmbed(string pasteBinURL, SocketGuildUser user)
		{
			Parser parser = new Parser();
			PasteBinFetcher pastebinFetcher = new PasteBinFetcher(_httpClient);

			string base64 = await pastebinFetcher.GetRawCodeAsync(pasteBinURL);
			Character character = parser.ParseCode(base64);
			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.AddField(GenerateDefenseField(character))
				.AddField(GenerateOffenseField(character))
				.WithFooter("Pastebin: " + pasteBinURL)
				.WithTitle((IsSupport(character) ? "Support" : character.Skills.MainSkillGroup.Gems.Where(x => x.Enabled).Select(x => x.Name).First()) + " - "
					+ (character.Ascendancy?.Length == 0 ? character.Class : character.Ascendancy) + " (Lvl: " + character.Level + ")")
				.WithThumbnailUrl("https://raw.githubusercontent.com/Kyle-Undefined/PoE-Bot/master/Resources/Images/Path-Of-Building/Classes/"
					+ (character.Ascendancy?.Length == 0 ? character.Class : character.Ascendancy) + ".png")
				.WithAuthor(user)
				.WithCurrentTimestamp();

			if (ShowCharges(character.Config) && (character.Summary.EnduranceCharges > 0 || character.Summary.PowerCharges > 0 || character.Summary.FrenzyCharges > 0))
				embed.AddField(GenerateChargesField(character));

			embed.AddField(GenerateSkillsField(character));

			if (character.Config.Contains("Input"))
				embed.AddField(GenerateConfigField(character));

			embed.AddField("**Info**:", "[Tree](" + character.Tree.Trim() + ") - powered by [Path of Building](https://github.com/Openarl/PathOfBuilding) - Help from "
				+ "[Faust](https://github.com/FWidm/discord-pob) and [thezensei](https://github.com/andreandersen/LiftDiscord/).");

			return embed.Build();
		}

		private double CalculateMaxDPS(float[] comparison)
		{
			double max = 0;

			foreach (var dps in comparison)
			{
				if (dps > max)
					max = dps;
			}

			return Math.Round(max, 2);
		}

		private double CheckThreshold(double stat) => CheckThreshold(stat, 0);

		private double CheckThreshold(double stat, double threshold)
		{
			if (stat >= threshold)
				return stat;

			return 0;
		}

		private string FirstLetterToUpper(string s)
		{
			if (string.IsNullOrEmpty(s))
				return string.Empty;

			char[] a = s.ToCharArray();
			a[0] = char.ToUpper(a[0]);
			return new string(a);
		}

		private EmbedFieldBuilder GenerateChargesField(Character character) => new EmbedFieldBuilder().WithName("Charges").WithValue(GenerateChargesString(character)).WithIsInline(false);

		private string GenerateChargesString(Character character)
		{
			var sb = new StringBuilder();
			var doc = XDocument.Parse(character.Config);
			var items = doc.Root.Element("Config").Elements("Input");
			var usePC = false;
			var useEC = false;
			var useFC = false;

			foreach (var item in items)
			{
				if (item.Attribute("name").Value is "usePowerCharges")
				{
					if (item.Attribute("boolean").Value is "true")
						usePC = true;
				}
				else if (item.Attribute("name").Value is "useFrenzyCharges")
				{
					if (item.Attribute("boolean").Value is "true")
						useFC = true;
				}
				else if (item.Attribute("name").Value is "useEnduranceCharges")
				{
					if (item.Attribute("boolean").Value is "true")
						useEC = true;
				}
			}

			string[] charge_types = { "Endurance", "Power", "Frenzy" };
			float[] charge_values = { character.Summary.EnduranceCharges, character.Summary.PowerCharges, character.Summary.FrenzyCharges };
			float[] charge_max_values = { character.Summary.EnduranceChargesMax, character.Summary.PowerChargesMax, character.Summary.FrenzyChargesMax };
			bool[] use_charges = { useEC, usePC, useFC };

			for (int i = 0; i < charge_types.Length; i++)
			{
				if (use_charges[i])
					sb.Append(charge_types[i]).Append(": ").AppendFormat("{0:N0}", charge_values[i]).Append("/").AppendFormat("{0:N0}", charge_max_values[i]).Append(", ");
			}

			if (sb.Length > 0)
				return sb.Remove(sb.Length - 2, 2).ToString();
			else
				return sb.ToString();
		}

		private EmbedFieldBuilder GenerateConfigField(Character character) => new EmbedFieldBuilder().WithName("Config").WithValue(GenerateConfigString(character)).WithIsInline(false);

		private string GenerateConfigString(Character character)
		{
			var json = JObject.Parse("{\"utc-date\":\"2018-04-26T17:25:02.876954\",\"conf\":{\"conditionStationary\":{\"var\":\"conditionStationary\",\"label\":\"Are you always stationary?\",\"category\":\"player\",\"suppress\":\"conditionMoving\",\"abbreviation\":\"Stationary\"},\"conditionMoving\":{\"var\":\"conditionMoving\",\"label\":\"Are you always moving?\",\"category\":\"player\",\"suppress\":\"conditionStationary\",\"abbreviation\":\"Moving\"},\"conditionFullLife\":{\"var\":\"conditionFullLife\",\"label\":\"Are you always on Full Life?\",\"category\":\"player\",\"suppress\":\"conditionLowLife\",\"abbreviation\":\"Full Life\"},\"conditionLowLife\":{\"var\":\"conditionLowLife\",\"label\":\"Are you always on Low Life?\",\"suppress\":\"conditionFullLife\",\"category\":\"player\",\"abbreviation\":\"Low Life\"},\"conditionFullEnergyShield\":{\"var\":\"conditionFullEnergyShield\",\"label\":\"Are you always on Full Energy Shield?\",\"suppress\":\"conditionHaveEnergyShield\",\"category\":\"player\",\"abbreviation\":\"Full ES\"},\"conditionHaveEnergyShield\":{\"var\":\"conditionHaveEnergyShield\",\"label\":\"Do you always have Energy Shield?\",\"category\":\"player\",\"abbreviation\":\"Has ES\"},\"minionsConditionFullLife\":{\"var\":\"minionsConditionFullLife\",\"label\":\"Are your minions always on Full Life?\",\"category\":\"playerSkill\",\"abbreviation\":\"Minions Full Life\"},\"iceNovaCastOnFrostbolt\":{\"var\":\"iceNovaCastOnFrostbolt\",\"label\":\"Cast on Frostbolt?\",\"category\":\"playerSkill\",\"abbreviation\":\"Nova on FB\"},\"innervateInnervation\":{\"var\":\"innervateInnervation\",\"label\":\"Is Innervation active?\",\"category\":\"player\",\"abbreviation\":\"Innervation\"},\"vortexCastOnFrostbolt\":{\"var\":\"vortexCastOnFrostbolt\",\"label\":\"Cast on Frostbolt?\",\"category\":\"playerSkill\",\"abbreviation\":\"Vortex on FB\"},\"usePowerCharges\":{\"var\":\"usePowerCharges\",\"label\":\"Do you use Power Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"PC\"},\"useFrenzyCharges\":{\"var\":\"useFrenzyCharges\",\"label\":\"Do you use Frenzy Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"FC\"},\"useEnduranceCharges\":{\"var\":\"useEnduranceCharges\",\"label\":\"Do you use Endurance Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"EC\"},\"useSiphoningCharges\":{\"var\":\"useSiphoningCharges\",\"label\":\"Do you use Siphoning Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Siphoning Charges\"},\"minionsUsePowerCharges\":{\"var\":\"minionsUsePowerCharges\",\"label\":\"Do your minions use Power Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Minion PC\"},\"minionsUseFrenzyCharges\":{\"var\":\"minionsUseFrenzyCharges\",\"label\":\"Do your minions use Frenzy Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Minion FC\"},\"minionsUseEnduranceCharges\":{\"var\":\"minionsUseEnduranceCharges\",\"label\":\"Do your minions use Endur. Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Minion EC\"},\"buffOnslaught\":{\"var\":\"buffOnslaught\",\"label\":\"Do you have Onslaught?\",\"category\":\"player\",\"abbreviation\":\"Onslaught\"},\"buffUnholyMight\":{\"var\":\"buffUnholyMight\",\"label\":\"Do you have Unholy Might?\",\"category\":\"player\",\"abbreviation\":\"Unholy Might\"},\"buffPhasing\":{\"var\":\"buffPhasing\",\"label\":\"Do you have Phasing?\",\"category\":\"player\",\"abbreviation\":\"Phasing\"},\"buffFortify\":{\"var\":\"buffFortify\",\"label\":\"Do you have Fortify?\",\"category\":\"player\",\"abbreviation\":\"Fortify\"},\"buffTailwind\":{\"var\":\"buffTailwind\",\"label\":\"Do you have Tailwind?\",\"category\":\"player\",\"abbreviation\":\"Tailwind\"},\"buffAdrenaline\":{\"var\":\"buffAdrenaline\",\"label\":\"Do you have Adrenaline?\",\"category\":\"player\",\"abbreviation\":\"Adrenaline\"},\"multiplierRage\":{\"var\":\"multiplierRage\",\"label\":\"Rage:\",\"category\":\"player\",\"abbreviation\":\"Rage\"},\"conditionLeeching\":{\"var\":\"conditionLeeching\",\"label\":\"Are you Leeching?\",\"category\":\"player\",\"abbreviation\":\"Leeching\"},\"conditionUsingFlask\":{\"var\":\"conditionUsingFlask\",\"label\":\"Do you have a Flask active?\",\"category\":\"player\",\"abbreviation\":\"Flasked\"},\"conditionHaveTotem\":{\"var\":\"conditionHaveTotem\",\"label\":\"Do you have a Totem summoned?\",\"category\":\"player\",\"abbreviation\":\"Totemed\"},\"conditionOnConsecratedGround\":{\"var\":\"conditionOnConsecratedGround\",\"label\":\"Are you on Consecrated Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Consecrated Ground\"},\"conditionOnBurningGround\":{\"var\":\"conditionOnBurningGround\",\"label\":\"Are you on Burning Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Burning Ground\"},\"conditionOnChilledGround\":{\"var\":\"conditionOnChilledGround\",\"label\":\"Are you on Chilled Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Chilled Ground\"},\"conditionOnShockedGround\":{\"var\":\"conditionOnShockedGround\",\"label\":\"Are you on Shocked Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Shocked Ground\"},\"conditionIgnited\":{\"var\":\"conditionIgnited\",\"label\":\"Are you Ignited?\",\"category\":\"playerAilment\",\"abbreviation\":\"Ignited\"},\"conditionChilled\":{\"var\":\"conditionChilled\",\"label\":\"Are you Chilled?\",\"category\":\"playerAilment\",\"abbreviation\":\"Chilled\"},\"conditionFrozen\":{\"var\":\"conditionFrozen\",\"label\":\"Are you Frozen?\",\"category\":\"playerAilment\",\"abbreviation\":\"Frozen\"},\"conditionShocked\":{\"var\":\"conditionShocked\",\"label\":\"Are you Shocked?\",\"category\":\"playerAilment\",\"abbreviation\":\"Shocked\"},\"conditionBleeding\":{\"var\":\"conditionBleeding\",\"label\":\"Are you Bleeding?\",\"category\":\"playerAilment\",\"abbreviation\":\"Bleeding\"},\"conditionPoisoned\":{\"var\":\"conditionPoisoned\",\"label\":\"Are you Poisoned?\",\"category\":\"playerAilment\",\"abbreviation\":\"Poisoned\"},\"multiplierPoisonOnSelf\":{\"var\":\"multiplierPoisonOnSelf\",\"label\":\"# of Poison on You:\",\"category\":\"playerAilment\",\"abbreviation\":\"Poisons on Self\"},\"conditionOnlyOneNearbyEnemy\":{\"var\":\"conditionOnlyOneNearbyEnemy\",\"label\":\"Is there only one nearby Enemy?\",\"category\":\"player\",\"abbreviation\":\"Single Enemy\"},\"conditionHitRecently\":{\"var\":\"conditionHitRecently\",\"label\":\"Have you Hit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit\"},\"conditionCritRecently\":{\"var\":\"conditionCritRecently\",\"label\":\"Have you Crit Recently?\",\"category\":\"playerRecently\",\"suppress\":\"conditionHitRecently\",\"abbreviation\":\"Crit\"},\"conditionNonCritRecently\":{\"var\":\"conditionNonCritRecently\",\"label\":\"Have you dealt a Non-Crit Recently?\",\"suppress\":\"conditionHitRecently\",\"category\":\"playerRecently\",\"abbreviation\":\"Non-Crit\"},\"conditionKilledRecently\":{\"var\":\"conditionKilledRecently\",\"label\":\"Have you Killed Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Killed\"},\"conditionTotemsKilledRecently\":{\"var\":\"conditionTotemsKilledRecently\",\"label\":\"Have your Totems Killed Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Totems Killed\"},\"conditionMinionsKilledRecently\":{\"var\":\"conditionMinionsKilledRecently\",\"label\":\"Have your Minions Killed Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Minions Killed\"},\"conditionKilledAffectedByDoT\":{\"var\":\"conditionKilledAffectedByDoT\",\"label\":\"Killed Enemy affected by your DoT Recently?\",\"category\":\"playerRecently\",\"suppress\":\"conditionKilledRecently\",\"abbreviation\":\"Killed DoT Affected\"},\"multiplierShockedEnemyKilledRecently\":{\"var\":\"multiplierShockedEnemyKilledRecently\",\"label\":\"# of Shocked Enemies Killed Recently:\",\"category\":\"playerRecently\",\"suppress\":\"conditionKilledRecently\",\"abbreviation\":\"Killed Shocked Enemies\"},\"conditionFrozenEnemyRecently\":{\"var\":\"conditionFrozenEnemyRecently\",\"label\":\"Have you Frozen an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Frozen Enemy\"},\"conditionIgnitedEnemyRecently\":{\"var\":\"conditionIgnitedEnemyRecently\",\"label\":\"Have you Ignited an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Ignited Enemy\"},\"conditionShockedEnemyRecently\":{\"var\":\"conditionShockedEnemyRecently\",\"label\":\"Have you Shocked an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Shocked Enemy\"},\"multiplierPoisonAppliedRecently\":{\"var\":\"multiplierPoisonAppliedRecently\",\"label\":\"# of Poisons applied Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Applied Poisons\"},\"conditionBeenHitRecently\":{\"var\":\"conditionBeenHitRecently\",\"label\":\"Have you been Hit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Been Hit\"},\"conditionBeenCritRecently\":{\"var\":\"conditionBeenCritRecently\",\"label\":\"Have you been Crit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Been Crit\"},\"conditionBeenSavageHitRecently\":{\"var\":\"conditionBeenSavageHitRecently\",\"label\":\"Have you been Savage Hit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Been Savage Hit\"},\"conditionHitByFireDamageRecently\":{\"var\":\"conditionHitByFireDamageRecently\",\"label\":\"Have you been hit by Fire Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit by Fire\"},\"conditionHitByColdDamageRecently\":{\"var\":\"conditionHitByColdDamageRecently\",\"label\":\"Have you been hit by Cold Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit by Cold\"},\"conditionHitByLightningDamageRecently\":{\"var\":\"conditionHitByLightningDamageRecently\",\"label\":\"Have you been hit by Light. Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit by Lightning\"},\"conditionBlockedAttackRecently\":{\"var\":\"conditionBlockedAttackRecently\",\"label\":\"Have you Blocked an Attack Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked Attack\"},\"conditionBlockedSpellRecently\":{\"var\":\"conditionBlockedSpellRecently\",\"label\":\"Have you Blocked a Spell Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked Spell\"},\"buffPendulum\":{\"var\":\"buffPendulum\",\"label\":\"Is Pendulum of Destruction active?\",\"category\":\"player\",\"abbreviation\":\"Pendulum\"},\"buffConflux\":{\"var\":\"buffConflux\",\"label\":\"Conflux Buff:\",\"category\":\"player\",\"abbreviation\":\"Conflux\"},\"buffBastionOfHope\":{\"var\":\"buffBastionOfHope\",\"label\":\"Is Bastion of Hope active?\",\"category\":\"player\",\"abbreviation\":\"Bastion of Hope\"},\"buffHerEmbrace\":{\"var\":\"buffHerEmbrace\",\"label\":\"Are you in Her Embrace?\",\"category\":\"player\",\"abbreviation\":\"Her Embrace\"},\"conditionAttackedRecently\":{\"var\":\"conditionAttackedRecently\",\"label\":\"Have you Attacked Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Attacked\"},\"conditionCastSpellRecently\":{\"var\":\"conditionCastSpellRecently\",\"label\":\"Have you Cast a Spell Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Cast\"},\"conditionUsedSkillRecently\":{\"var\":\"conditionUsedSkillRecently\",\"label\":\"Have you used a Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Skill\"},\"multiplierSkillUsedRecently\":{\"var\":\"multiplierSkillUsedRecently\",\"label\":\"# of Skills Used Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Skills used\"},\"conditionUsedWarcryRecently\":{\"var\":\"conditionUsedWarcryRecently\",\"label\":\"Have you used a Warcry Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Warcry\"},\"multiplierMineDetonatedRecently\":{\"var\":\"multiplierMineDetonatedRecently\",\"label\":\"# of Mines Detonated Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Mines Detonated\"},\"multiplierTrapTriggeredRecently\":{\"var\":\"multiplierTrapTriggeredRecently\",\"label\":\"# of Traps Triggered Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Traps Triggered\"},\"conditionConsumedCorpseRecently\":{\"var\":\"conditionConsumedCorpseRecently\",\"label\":\"Consumed a corpse Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Consumed Corpse\"},\"multiplierCorpseConsumedRecently\":{\"var\":\"multiplierCorpseConsumedRecently\",\"label\":\"# of Corpses Consumed Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Corpses Consumed\"},\"conditionTauntedEnemyRecently\":{\"var\":\"conditionTauntedEnemyRecently\",\"label\":\"Taunted an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Taunted Enemy\"},\"conditionUsedFireSkillRecently\":{\"var\":\"conditionUsedFireSkillRecently\",\"label\":\"Have you used a Fire Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Fire Skill\"},\"conditionUsedColdSkillRecently\":{\"var\":\"conditionUsedColdSkillRecently\",\"label\":\"Have you used a Cold Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Cold Skill\"},\"conditionUsedMinionSkillRecently\":{\"var\":\"conditionUsedMinionSkillRecently\",\"label\":\"Have you used a Minion Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Minion Skill\"},\"conditionUsedMovementSkillRecently\":{\"var\":\"conditionUsedMovementSkillRecently\",\"label\":\"Have you used a Movement Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Movement Skill\"},\"conditionUsedFireSkillInPast10Sec\":{\"var\":\"conditionUsedFireSkillInPast10Sec\",\"label\":\"Have you used a Fire Skill in the past 10s?\",\"category\":\"playerRecently\",\"suppress\":\"conditionUsedColdSkillRecently\",\"abbreviation\":\"Used Fire Skill (10s)\"},\"conditionUsedColdSkillInPast10Sec\":{\"var\":\"conditionUsedColdSkillInPast10Sec\",\"label\":\"Have you used a Cold Skill in the past 10s?\",\"category\":\"playerRecently\",\"suppress\":\"conditionUsedColdSkillRecently\",\"abbreviation\":\"Used Cold Skill (10s)\"},\"conditionUsedLightningSkillInPast10Sec\":{\"var\":\"conditionUsedLightningSkillInPast10Sec\",\"label\":\"Have you used a Light. Skill in the past 10s?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Light. Skill (10s)\"},\"conditionBlockedHitFromUniqueEnemyRecently\":{\"var\":\"conditionBlockedHitFromUniqueEnemyRecently\",\"label\":\"Blocked hit from a Unique Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked hit from a Unique\"},\"conditionBlockedHitFromUniqueEnemyInPast10Sec\":{\"var\":\"conditionBlockedHitFromUniqueEnemyInPast10Sec\",\"label\":\"Blocked hit from a Unique in the past 10s?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked hit from a Unique (10s)\"},\"critChanceLucky\":{\"var\":\"critChanceLucky\",\"label\":\"Is your Crit Chance Lucky?\",\"category\":\"player\",\"abbreviation\":\"Lucky Crits\"},\"skillChainCount\":{\"var\":\"skillChainCount\",\"label\":\"# of times Skill has Chained:\",\"category\":\"playerSkill\",\"abbreviation\":\"Chains\"},\"conditionEnemyMoving\":{\"var\":\"conditionEnemyMoving\",\"label\":\"Is the enemy Moving?\",\"category\":\"enemy\",\"abbreviation\":\"Moving\"},\"conditionEnemyFullLife\":{\"var\":\"conditionEnemyFullLife\",\"label\":\"Is the enemy on Full Life?\",\"suppress\":\"conditionEnemyFullLife\",\"category\":\"enemy\",\"abbreviation\":\"Full Life\"},\"conditionEnemyLowLife\":{\"var\":\"conditionEnemyLowLife\",\"label\":\"Is the enemy on Low Life?\",\"suppress\":\"conditionEnemyFullLife\",\"category\":\"enemy\",\"abbreviation\":\"Low Life\"},\"conditionAtCloseRange\":{\"var\":\"conditionAtCloseRange\",\"label\":\"Is the enemy at Close Range?\",\"category\":\"enemy\",\"abbreviation\":\"Close Range\"},\"conditionEnemyCursed\":{\"var\":\"conditionEnemyCursed\",\"label\":\"Is the enemy Cursed?\",\"category\":\"enemy\",\"abbreviation\":\"Cursed\"},\"conditionEnemyBleeding\":{\"var\":\"conditionEnemyBleeding\",\"label\":\"Is the enemy Bleeding?\",\"category\":\"enemy\",\"abbreviation\":\"Bleeding\"},\"conditionEnemyPoisoned\":{\"var\":\"conditionEnemyPoisoned\",\"label\":\"Is the enemy Poisoned?\",\"category\":\"enemy\",\"abbreviation\":\"Poisoned\"},\"multiplierPoisonOnEnemy\":{\"var\":\"multiplierPoisonOnEnemy\",\"label\":\"# of Poison on Enemy:\",\"category\":\"enemy\",\"abbreviation\":\"Poisons on Enemy\"},\"conditionEnemyMaimed\":{\"var\":\"conditionEnemyMaimed\",\"label\":\"Is the enemy Maimed?\",\"category\":\"enemy\",\"abbreviation\":\"Maimed\"},\"conditionEnemyHindered\":{\"var\":\"conditionEnemyHindered\",\"label\":\"Is the enemy Hindered?\",\"category\":\"enemy\",\"abbreviation\":\"Hindered\"},\"conditionEnemyBlinded\":{\"var\":\"conditionEnemyBlinded\",\"label\":\"Is the enemy Blinded?\",\"category\":\"enemy\",\"abbreviation\":\"Blinded\"},\"conditionEnemyTaunted\":{\"var\":\"conditionEnemyTaunted\",\"label\":\"Is the enemy Taunted?\",\"category\":\"enemy\",\"abbreviation\":\"Taunted\"},\"conditionEnemyBurning\":{\"var\":\"conditionEnemyBurning\",\"label\":\"Is the enemy Burning?\",\"category\":\"enemy\",\"abbreviation\":\"Burning\"},\"conditionEnemyIgnited\":{\"var\":\"conditionEnemyIgnited\",\"label\":\"Is the enemy Ignited?\",\"category\":\"enemy\",\"abbreviation\":\"Ignited\"},\"conditionEnemyChilled\":{\"var\":\"conditionEnemyChilled\",\"label\":\"Is the enemy Chilled?\",\"category\":\"enemy\",\"abbreviation\":\"Chilled\"},\"conditionEnemyFrozen\":{\"var\":\"conditionEnemyFrozen\",\"label\":\"Is the enemy Frozen?\",\"category\":\"enemy\",\"abbreviation\":\"Frozen\"},\"conditionEnemyShocked\":{\"var\":\"conditionEnemyShocked\",\"label\":\"Is the enemy Shocked?\",\"category\":\"enemy\",\"abbreviation\":\"Shocked\"},\"multiplierFreezeShockIgniteOnEnemy\":{\"var\":\"multiplierFreezeShockIgniteOnEnemy\",\"label\":\"# of Freeze/Shock/Ignite on Enemy:\",\"category\":\"enemy\",\"abbreviation\":\"Ele. Ailments on Enemy\"},\"conditionEnemyIntimidated\":{\"var\":\"conditionEnemyIntimidated\",\"label\":\"Is the enemy Intimidated?\",\"category\":\"enemy\",\"abbreviation\":\"Intimidated\"},\"conditionEnemyCoveredInAsh\":{\"var\":\"conditionEnemyCoveredInAsh\",\"label\":\"Is the enemy covered in Ash?\",\"category\":\"enemy\",\"abbreviation\":\"Ash\"},\"conditionEnemyRareOrUnique\":{\"var\":\"conditionEnemyRareOrUnique\",\"label\":\"is the enemy Rare or Unique?\",\"category\":\"enemy\",\"abbreviation\":\"Enemy Rare or Unique\"},\"enemyIsBoss\":{\"var\":\"enemyIsBoss\",\"label\":\"Is the enemy a Boss?\",\"category\":\"enemy\",\"abbreviation\":\"Boss\"},\"enemyConditionHitByFireDamage\":{\"var\":\"enemyConditionHitByFireDamage\",\"label\":\"Enemy was Hit by Fire Damage?\",\"category\":\"enemy\",\"abbreviation\":\"EE (Fire)\"},\"enemyConditionHitByColdDamage\":{\"var\":\"enemyConditionHitByColdDamage\",\"label\":\"Enemy was Hit by Cold Damage?\",\"category\":\"enemy\",\"abbreviation\":\"EE (Cold)\"},\"enemyConditionHitByLightningDamage\":{\"var\":\"enemyConditionHitByLightningDamage\",\"label\":\"Enemy was Hit by Light. Damage?\",\"category\":\"enemy\",\"abbreviation\":\"EE (Light)\"},\"aspectOfTheAvianAviansMight\":{\"var\":\"aspectOfTheAvianAviansMight\",\"label\":\"Is Avian's Might active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Avian's Might\"},\"aspectOfTheAvianAviansFlight\":{\"var\":\"aspectOfTheAvianAviansFlight\",\"label\":\"Is Avian's Flight active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Avian's Flight\"},\"aspectOfTheCatCatsStealth\":{\"var\":\"aspectOfTheCatCatsStealth\",\"label\":\"Is Cat's Stealth active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Cat's Stealth\"},\"aspectOfTheCatCatsAgility\":{\"var\":\"aspectOfTheCatCatsAgility\",\"label\":\"Is Cat's Agility active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Cat's Agility\"},\"overrideCrabBarriers\":{\"var\":\"overrideCrabBarriers\",\"label\":\"# of Crab Barriers (if not maximum):\",\"category\":\"overrides  \",\"abbreviation\":\"Crab Barriers\"},\"overridePowerCharges\":{\"var\":\"overridePowerCharges\",\"label\":\"# of Power Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#PC\"},\"overrideFrenzyCharges\":{\"var\":\"overrideFrenzyCharges\",\"label\":\"# of Frenzy Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#FC\"},\"overrideEnduranceCharges\":{\"var\":\"overrideEnduranceCharges\",\"label\":\"# of Endurance Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#EC\"},\"overrideSiphoningCharges\":{\"var\":\"overrideSiphoningCharges\",\"label\":\"# of Siphoning Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#SC\"},\"conditionBurning\":{\"var\":\"conditionBurning\",\"label\":\"Are you Burning?\",\"category\":\"player\",\"abbreviation\":\"Burning\"},\"conditionBlockedRecently\":{\"var\":\"conditionBlockedRecently\",\"label\":\"Have you Blocked Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked Recently\"},\"conditionEnergyShieldRechargeRecently\":{\"var\":\"conditionEnergyShieldRechargeRecently\",\"label\":\"Energy Shield Recharge started Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"ES Recharged recently\"},\"projectileDistance\":{\"var\":\"projectileDistance\",\"label\":\"Projectile travel distance:\",\"category\":\"playerSkill\",\"abbreviation\":\"Proj. Distance\"}}}");
			var config = new List<Config>();
			var doc = XDocument.Parse(character.Config);
			foreach (var input in doc.Root.Element("Config").Elements("Input"))
			{
				var attrs = input.Attributes().ToList();

				foreach (JProperty entry in json.SelectToken("conf"))
				{
					string var = entry.Value.SelectToken("var").ToString();
					string abbreviation = entry.Value.SelectToken("abbreviation").ToString();
					string category = entry.Value.SelectToken("category").ToString();

					if (var == attrs[0].Value)
						config.Add(new Config(var, abbreviation, category, attrs[1].Value));
				}
			}

			var configs = (Lookup<string, string>)config.ToLookup(x => FirstLetterToUpper(x.Category), x => PrepConfigLine(x.Abbreviation, x.Value));
			var sb = new StringBuilder();

			foreach (var configsGroup in configs)
			{
				if (configsGroup.Key.Length > 0)
				{
					var values = new List<string>();
					foreach (string str in configsGroup)
						values.Add(str);

					sb.Append("**").Append(configsGroup.Key).Append("**: ").Append(string.Join(", ", values)).Append("\n");
				}
			}

			return sb.ToString();
		}

		private string GenerateDamageString(Character character, double dps, double avg)
		{
			var sb = new StringBuilder();
			var speed = character.Summary.Speed;

			if (ShowAvgDamage(character) || avg > dps)
				sb.Append("**AVG**: ").AppendFormat("{0:N0}", avg).Append("\n");
			else
				sb.Append("**DPS**: ").AppendFormat("{0:N0}", dps).Append(" @ ").Append(speed > 0 ? Math.Round(speed, 2) : 0).Append("/s\n");

			var crit_chance = CheckThreshold(character.Summary.CritChance);
			var crit_multi = character.Summary.CritMultiplier;
			if (crit_chance > 0 && crit_chance > OutputThresholds.CRIT_CHANCE)
				sb.Append("**Crit**: Chance ").AppendFormat("{0:N2}", crit_chance).Append("% | Multiplier: ").AppendFormat("{0:N0}", crit_multi > 0 ? crit_multi * 100 : 150).Append("%\n");

			var acc = CheckThreshold(character.Summary.HitChance);
			if (acc < OutputThresholds.ACCURACY)
				sb.Append("**Hit Chance**: ").AppendFormat("{0:N2}", acc).Append("%");

			return sb.ToString();
		}

		private EmbedFieldBuilder GenerateDefenseField(Character character) => new EmbedFieldBuilder().WithName("Defense").WithValue(GenerateDefenseString(character)).WithIsInline(false);

		private string GenerateDefenseString(Character character)
		{
			var sb = new StringBuilder();

			var life_percent_threshold = Math.Min(OutputThresholds.LIFE_PERCENT, OutputThresholds.LIFE_PERCENT_PER_LEVEL * character.Level);
			var life_flat = character.Summary.Life;
			sb.Append(GenerateStatLine("Life", life_flat, CheckThreshold(character.Summary.LifeInc, life_percent_threshold), character.Summary.LifeUnreserved, character.Summary.LifeRegen, CheckThreshold(character.Summary.LifeLeechGainRate, life_flat > 0 ? life_flat * OutputThresholds.LEECH : 0)));

			var es_percent_threshold = Math.Min(OutputThresholds.ES_PERCENT, OutputThresholds.ES_PERCENT_PER_LEVEL * character.Level);
			var es_flat = character.Summary.EnergyShield;
			sb.Append(GenerateStatLine("Energy Shield", es_flat, CheckThreshold(character.Summary.EnergyShieldInc, es_percent_threshold), 0, character.Summary.EnergyShieldRegen, CheckThreshold(character.Summary.EnergyShieldLeechGainRate, es_flat > 0 ? es_flat * OutputThresholds.LEECH : 0)));

			if (character.Summary.NetLifeRegen > 0)
				sb.Append("**Net Regen**: ").AppendFormat("{0:N0}", character.Summary.NetLifeRegen).Append("/s\n");

			var mana_flat = character.Summary.Mana;
			sb.Append(GenerateStatLine("Mana", mana_flat, character.Summary.ManaInc, character.Summary.ManaUnreserved, character.Summary.ManaRegen, CheckThreshold(character.Summary.ManaLeechGainRate, mana_flat > 0 ? mana_flat * OutputThresholds.LEECH : 0)));

			// Secondary Defense Stats
			var ssb = new StringBuilder();
			var effective_life = Math.Max(character.Summary.Life, character.Summary.EnergyShield);

			var armour = CheckThreshold(character.Summary.Armour, Math.Min(OutputThresholds.ARMOUR, effective_life));
			if (armour > 0)
				ssb.Append("Armour: ").AppendFormat("{0:N0}", armour).Append(" | ");

			var evasion = CheckThreshold(character.Summary.Evasion, Math.Min(OutputThresholds.EVASION, effective_life));
			if (evasion > 0)
				ssb.Append("Evasion: ").AppendFormat("{0:N0}", evasion).Append(" | ");

			var dodge = CheckThreshold(character.Summary.AttackDodgeChance, OutputThresholds.DODGE);
			if (dodge > 0)
				ssb.Append("Dodge: ").AppendFormat("{0:N0}", dodge).Append("% | ");

			var spell_dodge = CheckThreshold(character.Summary.SpellDodgeChance, OutputThresholds.SPELL_DODGE);
			if (spell_dodge > 0)
				ssb.Append("Spell Dodge: ").AppendFormat("{0:N0}", spell_dodge).Append(" | ");

			var block = CheckThreshold(character.Summary.BlockChance, OutputThresholds.BLOCK);
			if (block > 0)
				ssb.Append("Block: ").AppendFormat("{0:N0}", block).Append("% | ");

			var spell_block = CheckThreshold(character.Summary.SpellBlockChance, OutputThresholds.SPELL_BLOCK);
			if (spell_block > 0)
				ssb.Append("Spell Block: ").AppendFormat("{0:N0}", spell_block).Append("% | ");

			if (ssb.Length > 0)
				sb.Append("**Secondary**: ").Append(ssb.Remove(ssb.Length - 3, 3).ToString()).Append("\n");

			// Resists
			var rsb = new StringBuilder();
			string[] resistenceNames = { "Fire", "Cold", "Lightning", "Chaos" };
			int[] resistenceValues = { character.Summary.FireResist, character.Summary.ColdResist, character.Summary.LightningResist, character.Summary.ChaosResist };
			int[] resistenceOverCap = { character.Summary.FireResistOverCap, character.Summary.ColdResistOverCap, character.Summary.LightningResistOverCap, character.Summary.ChaosResistOverCap };
			string[] emojis = { ":fire:", ":snowflake:", ":zap:", ":skull:" };
			var show = false;

			for (int i = 0; i < resistenceNames.Length; i++)
			{
				var res_val = CheckThreshold(resistenceValues[i], resistenceNames[i] is "Chaos" ? OutputThresholds.CHAOS_RES : OutputThresholds.ELE_RES);
				var res_overcap = resistenceOverCap[i];

				if (res_val > 0)
				{
					rsb.Append(emojis[i]).Append(" ").AppendFormat("{0:N0}", res_val);
					show = true;
					if (res_overcap > 0)
						rsb.Append("(+").AppendFormat("{0:N0}", res_overcap).Append(") ");
					rsb.Append(" ");
				}
			}

			rsb.AppendLine();

			if (show)
				sb.Append("**Resistances**: ").Append(rsb.ToString()).Append("\n");

			return sb.ToString();
		}

		private EmbedFieldBuilder GenerateOffenseField(Character character) =>
					new EmbedFieldBuilder().WithName(IsSupport(character) ? "Support" : "Offense").WithValue(GenerateOffenseString(character)).WithIsInline(false);

		private string GenerateOffenseString(Character character)
		{
			if (IsSupport(character))
				return "Auras: " + character.AuraCount + ", Curses: " + character.CurseCount;

			var sb = new StringBuilder();

			float[] comparison_dps = { character.Summary.TotalDPS, character.Summary.WithPoisonDPS, character.MinionSummary.TotalDPS, character.MinionSummary.WithPoisonDPS };
			float[] comparison_avg = { character.Summary.WithPoisonAverageDamage };
			var dps = CalculateMaxDPS(comparison_dps);
			var avg = CalculateMaxDPS(comparison_avg);

			if (dps > 0 || avg > 0)
				sb.Append(GenerateDamageString(character, dps, avg));

			return sb.ToString();
		}

		private EmbedFieldBuilder GenerateSkillsField(Character character) => new EmbedFieldBuilder().WithName("Main Skill").WithValue(GenerateSkillsString(character)).WithIsInline(false);

		private string GenerateSkillsString(Character character)
		{
			var sb = new StringBuilder();

			if (character.Skills.MainSkillGroup.IsEnabled)
			{
				foreach (Gem gem in character.Skills.MainSkillGroup.Gems)
				{
					if (gem.Enabled && !(gem.Name is "") && !(gem.Name.IndexOf("jewel", StringComparison.CurrentCultureIgnoreCase) >= 0))
						sb.Append(gem.Name).Append(" ").Append(gem.Quality > 0 || gem.Level > 20 ? "(" + gem.Level + "/" + gem.Quality + ") + " : "+ ");
				}

				sb.Remove(sb.Length - 2, 2);

				if (!string.IsNullOrEmpty(character.Skills.MainSkillGroup.Slot))
				{
					var itemSlot = character.Skills.MainSkillGroup.Slot;
					var itemID = character.ItemSlots.Single(x => x.Name == itemSlot).ItemID;
					var item = character.Items.Single(x => x.ID == itemID).Content;
					var itemName = string.Empty;

					var nameMatch = Regex.Match(item, @"\s*Rarity:.*\n\s*(.*)\n", RegexOptions.IgnoreCase);
					if (nameMatch.Success)
						itemName = nameMatch.Groups[1].Value;
					else
						itemName = item.Split("\n")[0];

					var modMatch = Regex.Match(item, "({variant:([0-9,]*)}|)Socketed Gems are Supported by level ([0-9]*) ([a-zA-Z ]*)", RegexOptions.IgnoreCase);

					if (!string.IsNullOrEmpty(modMatch.Groups[4].Value) && !string.IsNullOrEmpty(modMatch.Groups[3].Value))
						sb.Append("\n(+ ").Append(modMatch.Groups[4].Value).Append(" (").Append(modMatch.Groups[3].Value).Append(") from: *").Append(itemName.Trim()).Append("*)");
				}
			}
			else
			{
				sb.Append("None Selected");
			}

			return sb.ToString();
		}

		private string GenerateStatLine(string name, double stat, double stat_percent, double stat_unreserved = 0, double stat_regen = 0, double stat_leech_rate = 0)
		{
			var sb = new StringBuilder();

			if (stat > 0)
			{
				sb.Append("**").Append(name).Append("**: ");
				if (stat_unreserved > 0 && (stat - stat_unreserved > 0))
					sb.AppendFormat("{0:N0}", stat_unreserved).Append("/");
				sb.AppendFormat("{0:N0}", stat);
				sb.Append(" (").AppendFormat("{0:N0}", stat_percent).Append("%)");
				if (stat_regen > 0)
					sb.Append(" | Reg: ").AppendFormat("{0:N0}", stat_regen).Append("/s (").AppendFormat("{0:N1}", (stat_regen / stat) * 100).Append("%)");
				if (stat_leech_rate > 0)
					sb.Append(" | Leech ").AppendFormat("{0:N0}", stat_leech_rate).Append("/s (").AppendFormat("{0:N1}", (stat_leech_rate / stat) * 100).Append("%)");

				sb.AppendLine();
			}

			return sb.ToString();
		}

		private bool IsSupport(Character character) => character.AuraCount > OutputThresholds.AURA_SUPPORT || character.CurseCount > OutputThresholds.CURSE_SUPPORT;

		private string PrepConfigLine(string abbrev, string value)
		{
			var conf = abbrev;
			if (!(value.ToLower() is "true"))
				conf += ": " + FirstLetterToUpper(value);
			return conf;
		}

		private bool ShowAvgDamage(Character character)
		{
			var showAvg = false;
			string[] avgList = { "mine", "trap", "firestorm", "icestorm" };

			if (character.Skills.MainSkillGroup.IsEnabled)
			{
				foreach (var gem in character.Skills.MainSkillGroup.Gems)
				{
					if (avgList.Contains(gem.Name.ToLower()))
						showAvg = true;
				}
			}

			return showAvg;
		}

		private bool ShowCharges(string xml)
		{
			var doc = XDocument.Parse(xml);
			foreach (var item in doc.Root.Element("Config").Elements("Input"))
			{
				if (item.Attribute("name").Value is "usePowerCharges" || item.Attribute("name").Value is "useFrenzyCharges" || item.Attribute("name").Value is "useEnduranceCharges")
				{
					if (item.Attribute("boolean").Value is "true")
						return true;
				}
			}

			return false;
		}

		private struct OutputThresholds
		{
			// Offense
			public const double ACCURACY = 99;

			// The amount below specifies the ratio of life to ev/ar: 100 life <> 80+ AR/EV is displayed
			public const double AR_EV_THRESHOLD_PERCENTAGE = 0.8;

			public const double ARMOUR = 5000;
			public const double AURA_SUPPORT = 3;

			// most shields have 25-30 base, so +10 should be easily doable, spellblock is lower
			public const double BLOCK = 40;

			// Show all positive chaos res
			public const double CHAOS_RES = 0;

			// Charges
			public const double CHARGE_MAXIMUM = 3;

			public const double CRIT_CHANCE = 5;

			// AURAS AND CURSES
			public const double CURSE_SUPPORT = 3;

			// 30 = Acro/Phase Acro, half of that is displayable
			public const double DODGE = 15;

			// Show ele res bigger than the 75 cap
			public const double ELE_RES = 76;

			public const double ES_FLAT = 300;
			public const double ES_PERCENT = 50;
			public const double ES_PERCENT_PER_LEVEL = 1.5;
			public const double ES_REGEN = 100;
			public const double EVASION = 5000;
			public const double LEECH = 0.1;

			// Basic Defense
			public const double LIFE_FLAT = 1000;

			public const double LIFE_PERCENT = 50;
			public const double LIFE_PERCENT_PER_LEVEL = 1.5;
			public const double LIFE_REGEN = 100;
			public const double SPELL_BLOCK = 20;
			public const double SPELL_DODGE = 15;
		}
	}
}