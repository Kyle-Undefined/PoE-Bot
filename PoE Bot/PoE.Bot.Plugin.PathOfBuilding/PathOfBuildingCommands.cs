using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Plugin.PathOfBuilding.Models;

namespace PoE.Bot.Plugin.PathOfBuilding
{
    public class PathOfBuildingCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.PathOfBuilding Module"; } }

        Parser _parser = new Parser();
        PasteBinFetcher _pastebinFetcher = new PasteBinFetcher();

        [Command("pob", "Parses the PasteBin export from Path of Building and shows the information about the build.", CheckPermissions = false)]
        public async Task PoB(CommandContext ctx,
            [ArgumentParameter("PasteBin URL", true)] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("You must enter a PasteBin URL.");

            try
            {
                var base64 = await _pastebinFetcher.GetRawCode(url);
                var character = _parser.ParseCode(base64);
                var embed =
                    this.PrepareEmbed(character, ctx.Message.Author, EmbedType.Info)
                    .AddField(GenerateDefenseField(character))
                    .AddField(GenerateOffenseField(character))
                    .WithFooter($"Pastebin: {url}");

                if (ShowCharges(character.Config) && (character.Summary.EnduranceCharges > 0 || character.Summary.PowerCharges > 0 || character.Summary.FrenzyCharges > 0))
                    embed.AddField(GenerateChargesField(character));

                embed.AddField(GenerateSkillsField(character));

                if (character.Config.Contains("Input"))
                    embed.AddField(GenerateConfigField(character));

                embed.AddField("**Info**:", $"[Tree]({GenerateTreeURL(character.Tree)}) - powered by [Path of Building](https://github.com/Openarl/PathOfBuilding) - Help from [Faust](https://github.com/FWidm/discord-pob) and [thezensei](https://github.com/andreandersen/LiftDiscord/).");

                await ctx.Channel.SendMessageAsync("", false, embed.Build());
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync($"Error fetching build data. Reason: {ex.Message}");
            }
        }

        private EmbedFieldBuilder GenerateDefenseField(Character character)
        {
            var field = new EmbedFieldBuilder();

            field.WithName("Defense")
                .WithValue(GenerateDefenseString(character))
                .WithIsInline(false);

            return field;
        }

        private EmbedFieldBuilder GenerateOffenseField(Character character)
        {
            var field = new EmbedFieldBuilder();

            field.WithName(IsSupport(character) ? "Support" : "Offense")
                .WithValue(GenerateOffenseString(character))
                .WithIsInline(false);

            return field;
        }

        private EmbedFieldBuilder GenerateChargesField(Character character)
        {
            var field = new EmbedFieldBuilder();

            field.WithName("Charges")
                .WithValue(GenerateChargesString(character))
                .WithIsInline(false);

            return field;
        }

        private EmbedFieldBuilder GenerateSkillsField(Character character)
        {
            var field = new EmbedFieldBuilder();

            field.WithName("Main Skill")
                .WithValue(GenerateSkillsString(character))
                .WithIsInline(false);

            return field;
        }

        private EmbedFieldBuilder GenerateConfigField(Character character)
        {
            var field = new EmbedFieldBuilder();

            field.WithName("Config")
                .WithValue(GenerateConfigString(character))
                .WithIsInline(false);

            return field;
        }

        private string GenerateDefenseString(Character character)
        {
            StringBuilder sb = new StringBuilder();

            var life_percent_threshold = Math.Min(OutputThresholds.LIFE_PERCENT, OutputThresholds.LIFE_PERCENT_PER_LEVEL * character.Level);
            var life_flat = character.Summary.Life;
            sb.Append(GenerateStatLine("Life", life_flat, CheckThreshold(character.Summary.LifeInc, life_percent_threshold), character.Summary.LifeUnreserved, character.Summary.LifeRegen, CheckThreshold(character.Summary.LifeLeechGainRate, life_flat > 0 ? life_flat * OutputThresholds.LEECH : 0)));

            var es_percent_threshold = Math.Min(OutputThresholds.ES_PERCENT, OutputThresholds.ES_PERCENT_PER_LEVEL * character.Level);
            var es_flat = character.Summary.EnergyShield;
            sb.Append(GenerateStatLine("Energy Shield", es_flat, CheckThreshold(character.Summary.EnergyShieldInc, es_percent_threshold), 0, character.Summary.EnergyShieldRegen, CheckThreshold(character.Summary.EnergyShieldLeechGainRate, es_flat > 0 ? es_flat * OutputThresholds.LEECH : 0)));

            if (character.Summary.NetLifeRegen > 0)
                sb.Append($"**Net Regen**: {character.Summary.NetLifeRegen:N0}/s\n");

            var mana_flat = character.Summary.Mana;
            sb.Append(GenerateStatLine("Mana", mana_flat, character.Summary.ManaInc, character.Summary.ManaUnreserved, character.Summary.ManaRegen, CheckThreshold(character.Summary.ManaLeechGainRate, mana_flat > 0 ? mana_flat * OutputThresholds.LEECH : 0)));

            // Secondary Defense Stats
            StringBuilder ssb = new StringBuilder();
            var effective_life = Math.Max(character.Summary.Life, character.Summary.EnergyShield);

            var armour = CheckThreshold(character.Summary.Armour, Math.Min(OutputThresholds.ARMOUR, effective_life));
            if (armour > 0)
                ssb.Append($"Armour: {armour:N0} | ");

            var evasion = CheckThreshold(character.Summary.Evasion, Math.Min(OutputThresholds.EVASION, effective_life));
            if (evasion > 0)
                ssb.Append($"Evasion: {evasion:N0} | ");

            var dodge = CheckThreshold(character.Summary.AttackDodgeChance, OutputThresholds.DODGE);
            if (dodge > 0)
                ssb.Append($"Dodge: {dodge:N0}% | ");

            var spell_dodge = CheckThreshold(character.Summary.SpellDodgeChance, OutputThresholds.SPELL_DODGE);
            if (spell_dodge > 0)
                ssb.Append($"Spell Dodge: {spell_dodge:N0} | ");

            var block = CheckThreshold(character.Summary.BlockChance, OutputThresholds.BLOCK);
            if (block > 0)
                ssb.Append($"Block: {block:N0}% | ");

            var spell_block = CheckThreshold(character.Summary.SpellBlockChance, OutputThresholds.SPELL_BLOCK);
            if (spell_block > 0)
                ssb.Append($"Spell Block: {spell_block:N0}% | ");

            if (ssb.Length > 0)
                sb.Append($"**Secondary**: {ssb.Remove(ssb.Length - 3, 3).ToString()}\n");

            // Resists
            StringBuilder rsb = new StringBuilder();
            string[] resistenceNames = { "Fire", "Cold", "Lightning", "Chaos" };
            int[] resistenceValues = { character.Summary.FireResist, character.Summary.ColdResist, character.Summary.LightningResist, character.Summary.ChaosResist };
            int[] resistenceOverCap = { character.Summary.FireResistOverCap, character.Summary.ColdResistOverCap, character.Summary.LightningResistOverCap, character.Summary.ChaosResistOverCap };
            string[] emojis = { ":fire:", ":snowflake:", ":zap:", ":skull:" };
            bool show = false;

            for(var i = 0; i < resistenceNames.Length; i++)
            {
                var res_val = CheckThreshold(resistenceValues[i], resistenceNames[i] == "Chaos" ? OutputThresholds.CHAOS_RES : OutputThresholds.ELE_RES);
                var res_overcap = resistenceOverCap[i];

                if(res_val >0)
                {
                    rsb.Append($"{emojis[i]} {res_val:N0}");
                    show = true;
                    if (res_overcap > 0)
                        rsb.Append($"(+{res_overcap:N0}) ");
                    rsb.Append(" ");
                }
            }

            rsb.AppendLine();

            if (show)
                sb.Append($"**Resistances**: {rsb.ToString()}\n");

            return sb.ToString();
        }

        private string GenerateOffenseString(Character character)
        {
            if (IsSupport(character))
                return $"Auras: {character.AuraCount}, Curses: {character.CurseCount}";

            StringBuilder sb = new StringBuilder();

            float[] comparison_dps = { character.Summary.TotalDPS, character.Summary.WithPoisonDPS, character.MinionSummary.TotalDPS, character.MinionSummary.WithPoisonDPS };
            float[] comparison_avg = { character.Summary.WithPoisonAverageDamage };
            var dps = CalculateMaxDPS(comparison_dps);
            var avg = CalculateMaxDPS(comparison_avg);

            if (dps > 0 || avg > 0)
                sb.Append(GenerateDamageString(character, dps, avg));

            return sb.ToString();
        }

        private string GenerateStatLine(string name, double stat, double stat_percent, double stat_unreserved = 0, double stat_regen = 0, double stat_leech_rate = 0)
        {
            StringBuilder sb = new StringBuilder();

            if(stat > 0)
            {
                sb.Append($"**{name}**: ");
                if (stat_unreserved > 0 && (stat - stat_unreserved > 0))
                    sb.Append($"{stat_unreserved:N0}/");
                sb.Append($"{stat:N0}");
                sb.Append($" ({stat_percent:N0}%)");
                if (stat_regen > 0)
                    sb.Append($" | Reg: {stat_regen:N0}/s ({(stat_regen / stat) * 100:N1}%)");
                if (stat_leech_rate > 0)
                    sb.Append($" | Leech {stat_leech_rate:N0}/s ({(stat_leech_rate / stat) * 100:N1}%)");

                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        private string GenerateDamageString(Character character, double dps, double avg)
        {
            StringBuilder sb = new StringBuilder();
            var speed = character.Summary.Speed;

            if (ShowAvgDamage(character) || avg > dps)
                sb.Append($"**AVG**: {avg:N0}\n");
            else
                sb.Append($"**DPS**: {dps:N0} @ {(speed > 0 ? Math.Round(speed, 2) : 0)}/s\n");

            var crit_chance = CheckThreshold(character.Summary.CritChance);
            var crit_multi = character.Summary.CritMultiplier;
            if (crit_chance > 0 && crit_chance > OutputThresholds.CRIT_CHANCE)
                sb.Append($"**Crit**: Chance {crit_chance:N2}% | Multiplier: {(crit_multi > 0 ? crit_multi * 100 : 150):N0}%\n");

            var acc = CheckThreshold(character.Summary.HitChance);
            if (acc < OutputThresholds.ACCURACY)
                sb.Append($"**Hit Chance**: {acc:N2}%");
            
            return sb.ToString();
        }

        private string GenerateChargesString(Character character)
        {
            StringBuilder sb = new StringBuilder();
            var doc = XDocument.Parse(character.Config);
            var items = doc.Root.Element("Config").Elements("Input");
            bool usePC = false;
            bool useEC = false;
            bool useFC = false;

            foreach (var item in items)
            {
                if (item.Attribute("name").Value == "usePowerCharges")
                {
                    if (item.Attribute("boolean").Value == "true")
                        usePC = true;
                }
                else if (item.Attribute("name").Value == "useFrenzyCharges")
                {
                    if (item.Attribute("boolean").Value == "true")
                        useFC = true;
                }
                else if (item.Attribute("name").Value == "useEnduranceCharges")
                {
                    if (item.Attribute("boolean").Value == "true")
                        useEC = true;
                }
            }

            string[] charge_types = { "Endurance", "Power", "Frenzy" };
            float[] charge_values = { character.Summary.EnduranceCharges, character.Summary.PowerCharges, character.Summary.FrenzyCharges };
            float[] charge_max_values = { character.Summary.EnduranceChargesMax, character.Summary.PowerChargesMax, character.Summary.FrenzyChargesMax };
            bool[] use_charges = { useEC, usePC, useFC };

            for(var i = 0; i < charge_types.Length; i++)
                if (use_charges[i])
                    sb.Append($"{charge_types[i]}: {charge_values[i]:N0}/{charge_max_values[i]:N0}, ");

            if (sb.Length > 0)
                return sb.Remove(sb.Length - 2, 2).ToString();
            else
                return sb.ToString();
        }

        private string GenerateSkillsString(Character character)
        {
            StringBuilder sb = new StringBuilder();

            if (character.Skills.MainSkillGroup.IsEnabled)
            {
                foreach(var gem in character.Skills.MainSkillGroup.Gems)
                    if (gem.Enabled && gem.Name != "" && !gem.Name.ToLower().Contains("jewel"))
                        sb.Append($"{gem.Name} {(gem.Quality > 0 || gem.Level > 20 ? "(" + gem.Level + "/" + gem.Quality + ") + " : "+ ")}");

                sb.Remove(sb.Length - 2, 2);

                if (!string.IsNullOrEmpty(character.Skills.MainSkillGroup.Slot))
                {
                    var itemSlot = character.Skills.MainSkillGroup.Slot;
                    var itemID = character.ItemSlots.Where(i => i.Name == itemSlot).Single().ItemID;
                    var item = character.Items.Where(i => i.ID == itemID).Single().Content;
                    string itemName = "";

                    Match nMatch = Regex.Match(item, @"\s*Rarity:.*\n\s*(.*)\n", RegexOptions.IgnoreCase);
                    if (nMatch.Success)
                        itemName = nMatch.Groups[1].Value;
                    else
                        itemName = item.Split("\n")[0];

                    Match sMatch = Regex.Match(item, @"({variant:([0-9,]*)}|)Socketed Gems are Supported by level ([0-9]*) ([a-zA-Z ]*)", RegexOptions.IgnoreCase);

                    if (!string.IsNullOrEmpty(sMatch.Groups[4].Value) && !string.IsNullOrEmpty(sMatch.Groups[3].Value))
                        sb.Append($"\n(+ {sMatch.Groups[4].Value} ({sMatch.Groups[3].Value}) from: *{itemName.Trim()}*)");
                }
            }
            else
                sb.Append("None Selected");

            return sb.ToString();
        }

        private string GenerateConfigString(Character character)
        {
            JObject json = JObject.Parse("{\"utc-date\":\"2018-04-26T17:25:02.876954\",\"conf\":{\"conditionStationary\":{\"var\":\"conditionStationary\",\"label\":\"Are you always stationary?\",\"category\":\"player\",\"suppress\":\"conditionMoving\",\"abbreviation\":\"Stationary\"},\"conditionMoving\":{\"var\":\"conditionMoving\",\"label\":\"Are you always moving?\",\"category\":\"player\",\"suppress\":\"conditionStationary\",\"abbreviation\":\"Moving\"},\"conditionFullLife\":{\"var\":\"conditionFullLife\",\"label\":\"Are you always on Full Life?\",\"category\":\"player\",\"suppress\":\"conditionLowLife\",\"abbreviation\":\"Full Life\"},\"conditionLowLife\":{\"var\":\"conditionLowLife\",\"label\":\"Are you always on Low Life?\",\"suppress\":\"conditionFullLife\",\"category\":\"player\",\"abbreviation\":\"Low Life\"},\"conditionFullEnergyShield\":{\"var\":\"conditionFullEnergyShield\",\"label\":\"Are you always on Full Energy Shield?\",\"suppress\":\"conditionHaveEnergyShield\",\"category\":\"player\",\"abbreviation\":\"Full ES\"},\"conditionHaveEnergyShield\":{\"var\":\"conditionHaveEnergyShield\",\"label\":\"Do you always have Energy Shield?\",\"category\":\"player\",\"abbreviation\":\"Has ES\"},\"minionsConditionFullLife\":{\"var\":\"minionsConditionFullLife\",\"label\":\"Are your minions always on Full Life?\",\"category\":\"playerSkill\",\"abbreviation\":\"Minions Full Life\"},\"iceNovaCastOnFrostbolt\":{\"var\":\"iceNovaCastOnFrostbolt\",\"label\":\"Cast on Frostbolt?\",\"category\":\"playerSkill\",\"abbreviation\":\"Nova on FB\"},\"innervateInnervation\":{\"var\":\"innervateInnervation\",\"label\":\"Is Innervation active?\",\"category\":\"player\",\"abbreviation\":\"Innervation\"},\"vortexCastOnFrostbolt\":{\"var\":\"vortexCastOnFrostbolt\",\"label\":\"Cast on Frostbolt?\",\"category\":\"playerSkill\",\"abbreviation\":\"Vortex on FB\"},\"usePowerCharges\":{\"var\":\"usePowerCharges\",\"label\":\"Do you use Power Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"PC\"},\"useFrenzyCharges\":{\"var\":\"useFrenzyCharges\",\"label\":\"Do you use Frenzy Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"FC\"},\"useEnduranceCharges\":{\"var\":\"useEnduranceCharges\",\"label\":\"Do you use Endurance Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"EC\"},\"useSiphoningCharges\":{\"var\":\"useSiphoningCharges\",\"label\":\"Do you use Siphoning Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Siphoning Charges\"},\"minionsUsePowerCharges\":{\"var\":\"minionsUsePowerCharges\",\"label\":\"Do your minions use Power Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Minion PC\"},\"minionsUseFrenzyCharges\":{\"var\":\"minionsUseFrenzyCharges\",\"label\":\"Do your minions use Frenzy Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Minion FC\"},\"minionsUseEnduranceCharges\":{\"var\":\"minionsUseEnduranceCharges\",\"label\":\"Do your minions use Endur. Charges?\",\"category\":\"playerCharge\",\"abbreviation\":\"Minion EC\"},\"buffOnslaught\":{\"var\":\"buffOnslaught\",\"label\":\"Do you have Onslaught?\",\"category\":\"player\",\"abbreviation\":\"Onslaught\"},\"buffUnholyMight\":{\"var\":\"buffUnholyMight\",\"label\":\"Do you have Unholy Might?\",\"category\":\"player\",\"abbreviation\":\"Unholy Might\"},\"buffPhasing\":{\"var\":\"buffPhasing\",\"label\":\"Do you have Phasing?\",\"category\":\"player\",\"abbreviation\":\"Phasing\"},\"buffFortify\":{\"var\":\"buffFortify\",\"label\":\"Do you have Fortify?\",\"category\":\"player\",\"abbreviation\":\"Fortify\"},\"buffTailwind\":{\"var\":\"buffTailwind\",\"label\":\"Do you have Tailwind?\",\"category\":\"player\",\"abbreviation\":\"Tailwind\"},\"buffAdrenaline\":{\"var\":\"buffAdrenaline\",\"label\":\"Do you have Adrenaline?\",\"category\":\"player\",\"abbreviation\":\"Adrenaline\"},\"multiplierRage\":{\"var\":\"multiplierRage\",\"label\":\"Rage:\",\"category\":\"player\",\"abbreviation\":\"Rage\"},\"conditionLeeching\":{\"var\":\"conditionLeeching\",\"label\":\"Are you Leeching?\",\"category\":\"player\",\"abbreviation\":\"Leeching\"},\"conditionUsingFlask\":{\"var\":\"conditionUsingFlask\",\"label\":\"Do you have a Flask active?\",\"category\":\"player\",\"abbreviation\":\"Flasked\"},\"conditionHaveTotem\":{\"var\":\"conditionHaveTotem\",\"label\":\"Do you have a Totem summoned?\",\"category\":\"player\",\"abbreviation\":\"Totemed\"},\"conditionOnConsecratedGround\":{\"var\":\"conditionOnConsecratedGround\",\"label\":\"Are you on Consecrated Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Consecrated Ground\"},\"conditionOnBurningGround\":{\"var\":\"conditionOnBurningGround\",\"label\":\"Are you on Burning Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Burning Ground\"},\"conditionOnChilledGround\":{\"var\":\"conditionOnChilledGround\",\"label\":\"Are you on Chilled Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Chilled Ground\"},\"conditionOnShockedGround\":{\"var\":\"conditionOnShockedGround\",\"label\":\"Are you on Shocked Ground?\",\"category\":\"playerAilment\",\"abbreviation\":\"Shocked Ground\"},\"conditionIgnited\":{\"var\":\"conditionIgnited\",\"label\":\"Are you Ignited?\",\"category\":\"playerAilment\",\"abbreviation\":\"Ignited\"},\"conditionChilled\":{\"var\":\"conditionChilled\",\"label\":\"Are you Chilled?\",\"category\":\"playerAilment\",\"abbreviation\":\"Chilled\"},\"conditionFrozen\":{\"var\":\"conditionFrozen\",\"label\":\"Are you Frozen?\",\"category\":\"playerAilment\",\"abbreviation\":\"Frozen\"},\"conditionShocked\":{\"var\":\"conditionShocked\",\"label\":\"Are you Shocked?\",\"category\":\"playerAilment\",\"abbreviation\":\"Shocked\"},\"conditionBleeding\":{\"var\":\"conditionBleeding\",\"label\":\"Are you Bleeding?\",\"category\":\"playerAilment\",\"abbreviation\":\"Bleeding\"},\"conditionPoisoned\":{\"var\":\"conditionPoisoned\",\"label\":\"Are you Poisoned?\",\"category\":\"playerAilment\",\"abbreviation\":\"Poisoned\"},\"multiplierPoisonOnSelf\":{\"var\":\"multiplierPoisonOnSelf\",\"label\":\"# of Poison on You:\",\"category\":\"playerAilment\",\"abbreviation\":\"Poisons on Self\"},\"conditionOnlyOneNearbyEnemy\":{\"var\":\"conditionOnlyOneNearbyEnemy\",\"label\":\"Is there only one nearby Enemy?\",\"category\":\"player\",\"abbreviation\":\"Single Enemy\"},\"conditionHitRecently\":{\"var\":\"conditionHitRecently\",\"label\":\"Have you Hit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit\"},\"conditionCritRecently\":{\"var\":\"conditionCritRecently\",\"label\":\"Have you Crit Recently?\",\"category\":\"playerRecently\",\"suppress\":\"conditionHitRecently\",\"abbreviation\":\"Crit\"},\"conditionNonCritRecently\":{\"var\":\"conditionNonCritRecently\",\"label\":\"Have you dealt a Non-Crit Recently?\",\"suppress\":\"conditionHitRecently\",\"category\":\"playerRecently\",\"abbreviation\":\"Non-Crit\"},\"conditionKilledRecently\":{\"var\":\"conditionKilledRecently\",\"label\":\"Have you Killed Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Killed\"},\"conditionTotemsKilledRecently\":{\"var\":\"conditionTotemsKilledRecently\",\"label\":\"Have your Totems Killed Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Totems Killed\"},\"conditionMinionsKilledRecently\":{\"var\":\"conditionMinionsKilledRecently\",\"label\":\"Have your Minions Killed Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Minions Killed\"},\"conditionKilledAffectedByDoT\":{\"var\":\"conditionKilledAffectedByDoT\",\"label\":\"Killed Enemy affected by your DoT Recently?\",\"category\":\"playerRecently\",\"suppress\":\"conditionKilledRecently\",\"abbreviation\":\"Killed DoT Affected\"},\"multiplierShockedEnemyKilledRecently\":{\"var\":\"multiplierShockedEnemyKilledRecently\",\"label\":\"# of Shocked Enemies Killed Recently:\",\"category\":\"playerRecently\",\"suppress\":\"conditionKilledRecently\",\"abbreviation\":\"Killed Shocked Enemies\"},\"conditionFrozenEnemyRecently\":{\"var\":\"conditionFrozenEnemyRecently\",\"label\":\"Have you Frozen an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Frozen Enemy\"},\"conditionIgnitedEnemyRecently\":{\"var\":\"conditionIgnitedEnemyRecently\",\"label\":\"Have you Ignited an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Ignited Enemy\"},\"conditionShockedEnemyRecently\":{\"var\":\"conditionShockedEnemyRecently\",\"label\":\"Have you Shocked an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Shocked Enemy\"},\"multiplierPoisonAppliedRecently\":{\"var\":\"multiplierPoisonAppliedRecently\",\"label\":\"# of Poisons applied Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Applied Poisons\"},\"conditionBeenHitRecently\":{\"var\":\"conditionBeenHitRecently\",\"label\":\"Have you been Hit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Been Hit\"},\"conditionBeenCritRecently\":{\"var\":\"conditionBeenCritRecently\",\"label\":\"Have you been Crit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Been Crit\"},\"conditionBeenSavageHitRecently\":{\"var\":\"conditionBeenSavageHitRecently\",\"label\":\"Have you been Savage Hit Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Been Savage Hit\"},\"conditionHitByFireDamageRecently\":{\"var\":\"conditionHitByFireDamageRecently\",\"label\":\"Have you been hit by Fire Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit by Fire\"},\"conditionHitByColdDamageRecently\":{\"var\":\"conditionHitByColdDamageRecently\",\"label\":\"Have you been hit by Cold Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit by Cold\"},\"conditionHitByLightningDamageRecently\":{\"var\":\"conditionHitByLightningDamageRecently\",\"label\":\"Have you been hit by Light. Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Hit by Lightning\"},\"conditionBlockedAttackRecently\":{\"var\":\"conditionBlockedAttackRecently\",\"label\":\"Have you Blocked an Attack Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked Attack\"},\"conditionBlockedSpellRecently\":{\"var\":\"conditionBlockedSpellRecently\",\"label\":\"Have you Blocked a Spell Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked Spell\"},\"buffPendulum\":{\"var\":\"buffPendulum\",\"label\":\"Is Pendulum of Destruction active?\",\"category\":\"player\",\"abbreviation\":\"Pendulum\"},\"buffConflux\":{\"var\":\"buffConflux\",\"label\":\"Conflux Buff:\",\"category\":\"player\",\"abbreviation\":\"Conflux\"},\"buffBastionOfHope\":{\"var\":\"buffBastionOfHope\",\"label\":\"Is Bastion of Hope active?\",\"category\":\"player\",\"abbreviation\":\"Bastion of Hope\"},\"buffHerEmbrace\":{\"var\":\"buffHerEmbrace\",\"label\":\"Are you in Her Embrace?\",\"category\":\"player\",\"abbreviation\":\"Her Embrace\"},\"conditionAttackedRecently\":{\"var\":\"conditionAttackedRecently\",\"label\":\"Have you Attacked Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Attacked\"},\"conditionCastSpellRecently\":{\"var\":\"conditionCastSpellRecently\",\"label\":\"Have you Cast a Spell Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Cast\"},\"conditionUsedSkillRecently\":{\"var\":\"conditionUsedSkillRecently\",\"label\":\"Have you used a Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Skill\"},\"multiplierSkillUsedRecently\":{\"var\":\"multiplierSkillUsedRecently\",\"label\":\"# of Skills Used Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Skills used\"},\"conditionUsedWarcryRecently\":{\"var\":\"conditionUsedWarcryRecently\",\"label\":\"Have you used a Warcry Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Warcry\"},\"multiplierMineDetonatedRecently\":{\"var\":\"multiplierMineDetonatedRecently\",\"label\":\"# of Mines Detonated Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Mines Detonated\"},\"multiplierTrapTriggeredRecently\":{\"var\":\"multiplierTrapTriggeredRecently\",\"label\":\"# of Traps Triggered Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Traps Triggered\"},\"conditionConsumedCorpseRecently\":{\"var\":\"conditionConsumedCorpseRecently\",\"label\":\"Consumed a corpse Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Consumed Corpse\"},\"multiplierCorpseConsumedRecently\":{\"var\":\"multiplierCorpseConsumedRecently\",\"label\":\"# of Corpses Consumed Recently:\",\"category\":\"playerRecently\",\"abbreviation\":\"Corpses Consumed\"},\"conditionTauntedEnemyRecently\":{\"var\":\"conditionTauntedEnemyRecently\",\"label\":\"Taunted an Enemy Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Taunted Enemy\"},\"conditionUsedFireSkillRecently\":{\"var\":\"conditionUsedFireSkillRecently\",\"label\":\"Have you used a Fire Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Fire Skill\"},\"conditionUsedColdSkillRecently\":{\"var\":\"conditionUsedColdSkillRecently\",\"label\":\"Have you used a Cold Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Cold Skill\"},\"conditionUsedMinionSkillRecently\":{\"var\":\"conditionUsedMinionSkillRecently\",\"label\":\"Have you used a Minion Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Minion Skill\"},\"conditionUsedMovementSkillRecently\":{\"var\":\"conditionUsedMovementSkillRecently\",\"label\":\"Have you used a Movement Skill Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Movement Skill\"},\"conditionUsedFireSkillInPast10Sec\":{\"var\":\"conditionUsedFireSkillInPast10Sec\",\"label\":\"Have you used a Fire Skill in the past 10s?\",\"category\":\"playerRecently\",\"suppress\":\"conditionUsedColdSkillRecently\",\"abbreviation\":\"Used Fire Skill (10s)\"},\"conditionUsedColdSkillInPast10Sec\":{\"var\":\"conditionUsedColdSkillInPast10Sec\",\"label\":\"Have you used a Cold Skill in the past 10s?\",\"category\":\"playerRecently\",\"suppress\":\"conditionUsedColdSkillRecently\",\"abbreviation\":\"Used Cold Skill (10s)\"},\"conditionUsedLightningSkillInPast10Sec\":{\"var\":\"conditionUsedLightningSkillInPast10Sec\",\"label\":\"Have you used a Light. Skill in the past 10s?\",\"category\":\"playerRecently\",\"abbreviation\":\"Used Light. Skill (10s)\"},\"conditionBlockedHitFromUniqueEnemyRecently\":{\"var\":\"conditionBlockedHitFromUniqueEnemyRecently\",\"label\":\"Blocked hit from a Unique Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked hit from a Unique\"},\"conditionBlockedHitFromUniqueEnemyInPast10Sec\":{\"var\":\"conditionBlockedHitFromUniqueEnemyInPast10Sec\",\"label\":\"Blocked hit from a Unique in the past 10s?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked hit from a Unique (10s)\"},\"critChanceLucky\":{\"var\":\"critChanceLucky\",\"label\":\"Is your Crit Chance Lucky?\",\"category\":\"player\",\"abbreviation\":\"Lucky Crits\"},\"skillChainCount\":{\"var\":\"skillChainCount\",\"label\":\"# of times Skill has Chained:\",\"category\":\"playerSkill\",\"abbreviation\":\"Chains\"},\"conditionEnemyMoving\":{\"var\":\"conditionEnemyMoving\",\"label\":\"Is the enemy Moving?\",\"category\":\"enemy\",\"abbreviation\":\"Moving\"},\"conditionEnemyFullLife\":{\"var\":\"conditionEnemyFullLife\",\"label\":\"Is the enemy on Full Life?\",\"suppress\":\"conditionEnemyFullLife\",\"category\":\"enemy\",\"abbreviation\":\"Full Life\"},\"conditionEnemyLowLife\":{\"var\":\"conditionEnemyLowLife\",\"label\":\"Is the enemy on Low Life?\",\"suppress\":\"conditionEnemyFullLife\",\"category\":\"enemy\",\"abbreviation\":\"Low Life\"},\"conditionAtCloseRange\":{\"var\":\"conditionAtCloseRange\",\"label\":\"Is the enemy at Close Range?\",\"category\":\"enemy\",\"abbreviation\":\"Close Range\"},\"conditionEnemyCursed\":{\"var\":\"conditionEnemyCursed\",\"label\":\"Is the enemy Cursed?\",\"category\":\"enemy\",\"abbreviation\":\"Cursed\"},\"conditionEnemyBleeding\":{\"var\":\"conditionEnemyBleeding\",\"label\":\"Is the enemy Bleeding?\",\"category\":\"enemy\",\"abbreviation\":\"Bleeding\"},\"conditionEnemyPoisoned\":{\"var\":\"conditionEnemyPoisoned\",\"label\":\"Is the enemy Poisoned?\",\"category\":\"enemy\",\"abbreviation\":\"Poisoned\"},\"multiplierPoisonOnEnemy\":{\"var\":\"multiplierPoisonOnEnemy\",\"label\":\"# of Poison on Enemy:\",\"category\":\"enemy\",\"abbreviation\":\"Poisons on Enemy\"},\"conditionEnemyMaimed\":{\"var\":\"conditionEnemyMaimed\",\"label\":\"Is the enemy Maimed?\",\"category\":\"enemy\",\"abbreviation\":\"Maimed\"},\"conditionEnemyHindered\":{\"var\":\"conditionEnemyHindered\",\"label\":\"Is the enemy Hindered?\",\"category\":\"enemy\",\"abbreviation\":\"Hindered\"},\"conditionEnemyBlinded\":{\"var\":\"conditionEnemyBlinded\",\"label\":\"Is the enemy Blinded?\",\"category\":\"enemy\",\"abbreviation\":\"Blinded\"},\"conditionEnemyTaunted\":{\"var\":\"conditionEnemyTaunted\",\"label\":\"Is the enemy Taunted?\",\"category\":\"enemy\",\"abbreviation\":\"Taunted\"},\"conditionEnemyBurning\":{\"var\":\"conditionEnemyBurning\",\"label\":\"Is the enemy Burning?\",\"category\":\"enemy\",\"abbreviation\":\"Burning\"},\"conditionEnemyIgnited\":{\"var\":\"conditionEnemyIgnited\",\"label\":\"Is the enemy Ignited?\",\"category\":\"enemy\",\"abbreviation\":\"Ignited\"},\"conditionEnemyChilled\":{\"var\":\"conditionEnemyChilled\",\"label\":\"Is the enemy Chilled?\",\"category\":\"enemy\",\"abbreviation\":\"Chilled\"},\"conditionEnemyFrozen\":{\"var\":\"conditionEnemyFrozen\",\"label\":\"Is the enemy Frozen?\",\"category\":\"enemy\",\"abbreviation\":\"Frozen\"},\"conditionEnemyShocked\":{\"var\":\"conditionEnemyShocked\",\"label\":\"Is the enemy Shocked?\",\"category\":\"enemy\",\"abbreviation\":\"Shocked\"},\"multiplierFreezeShockIgniteOnEnemy\":{\"var\":\"multiplierFreezeShockIgniteOnEnemy\",\"label\":\"# of Freeze/Shock/Ignite on Enemy:\",\"category\":\"enemy\",\"abbreviation\":\"Ele. Ailments on Enemy\"},\"conditionEnemyIntimidated\":{\"var\":\"conditionEnemyIntimidated\",\"label\":\"Is the enemy Intimidated?\",\"category\":\"enemy\",\"abbreviation\":\"Intimidated\"},\"conditionEnemyCoveredInAsh\":{\"var\":\"conditionEnemyCoveredInAsh\",\"label\":\"Is the enemy covered in Ash?\",\"category\":\"enemy\",\"abbreviation\":\"Ash\"},\"conditionEnemyRareOrUnique\":{\"var\":\"conditionEnemyRareOrUnique\",\"label\":\"is the enemy Rare or Unique?\",\"category\":\"enemy\",\"abbreviation\":\"Enemy Rare or Unique\"},\"enemyIsBoss\":{\"var\":\"enemyIsBoss\",\"label\":\"Is the enemy a Boss?\",\"category\":\"enemy\",\"abbreviation\":\"Boss\"},\"enemyConditionHitByFireDamage\":{\"var\":\"enemyConditionHitByFireDamage\",\"label\":\"Enemy was Hit by Fire Damage?\",\"category\":\"enemy\",\"abbreviation\":\"EE (Fire)\"},\"enemyConditionHitByColdDamage\":{\"var\":\"enemyConditionHitByColdDamage\",\"label\":\"Enemy was Hit by Cold Damage?\",\"category\":\"enemy\",\"abbreviation\":\"EE (Cold)\"},\"enemyConditionHitByLightningDamage\":{\"var\":\"enemyConditionHitByLightningDamage\",\"label\":\"Enemy was Hit by Light. Damage?\",\"category\":\"enemy\",\"abbreviation\":\"EE (Light)\"},\"aspectOfTheAvianAviansMight\":{\"var\":\"aspectOfTheAvianAviansMight\",\"label\":\"Is Avian's Might active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Avian's Might\"},\"aspectOfTheAvianAviansFlight\":{\"var\":\"aspectOfTheAvianAviansFlight\",\"label\":\"Is Avian's Flight active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Avian's Flight\"},\"aspectOfTheCatCatsStealth\":{\"var\":\"aspectOfTheCatCatsStealth\",\"label\":\"Is Cat's Stealth active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Cat's Stealth\"},\"aspectOfTheCatCatsAgility\":{\"var\":\"aspectOfTheCatCatsAgility\",\"label\":\"Is Cat's Agility active?\",\"category\":\"playerSkill\",\"abbreviation\":\"Cat's Agility\"},\"overrideCrabBarriers\":{\"var\":\"overrideCrabBarriers\",\"label\":\"# of Crab Barriers (if not maximum):\",\"category\":\"overrides  \",\"abbreviation\":\"Crab Barriers\"},\"overridePowerCharges\":{\"var\":\"overridePowerCharges\",\"label\":\"# of Power Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#PC\"},\"overrideFrenzyCharges\":{\"var\":\"overrideFrenzyCharges\",\"label\":\"# of Frenzy Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#FC\"},\"overrideEnduranceCharges\":{\"var\":\"overrideEnduranceCharges\",\"label\":\"# of Endurance Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#EC\"},\"overrideSiphoningCharges\":{\"var\":\"overrideSiphoningCharges\",\"label\":\"# of Siphoning Charges (if not maximum):\",\"category\":\"overrides\",\"abbreviation\":\"#SC\"},\"conditionBurning\":{\"var\":\"conditionBurning\",\"label\":\"Are you Burning?\",\"category\":\"player\",\"abbreviation\":\"Burning\"},\"conditionBlockedRecently\":{\"var\":\"conditionBlockedRecently\",\"label\":\"Have you Blocked Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"Blocked Recently\"},\"conditionEnergyShieldRechargeRecently\":{\"var\":\"conditionEnergyShieldRechargeRecently\",\"label\":\"Energy Shield Recharge started Recently?\",\"category\":\"playerRecently\",\"abbreviation\":\"ES Recharged recently\"},\"projectileDistance\":{\"var\":\"projectileDistance\",\"label\":\"Projectile travel distance:\",\"category\":\"playerSkill\",\"abbreviation\":\"Proj. Distance\"}}}");
            List<Config> config = new List<Config>();
            var doc = XDocument.Parse(character.Config);
            var inputs = doc.Root.Element("Config").Elements("Input");

            foreach(var input in inputs)
            {
                var attrs = input.Attributes().ToList();
                
                foreach(JProperty entry in json.SelectToken("conf"))
                {
                    var var = entry.Value.SelectToken("var").ToString();
                    var abbreviation = entry.Value.SelectToken("abbreviation").ToString();
                    var category = entry.Value.SelectToken("category").ToString();

                    if (var == attrs[0].Value)
                        config.Add(new Config(var, abbreviation, category, attrs[1].Value));
                }
            }

            Lookup<string, string> configs = (Lookup<string, string>)config.ToLookup(c => FirstLetterToUpper(c.Category), c => PrepConfigLine(c.Abbreviation, c.Value));

            StringBuilder sb = new StringBuilder();

            foreach (IGrouping<string, string> configsGroup in configs)
            {
                if (configsGroup.Key.Length > 0)
                {
                    List<string> values = new List<string>();
                    foreach (string str in configsGroup)
                        values.Add(str);

                    sb.Append($"**{configsGroup.Key}**: {string.Join(", ", values)}\n");
                }
            }

            return sb.ToString();
        }

        private bool ShowAvgDamage(Character character)
        {
            bool showAvg = false;
            string[] avgList = { "mine", "trap", "firestorm", "icestorm" };

            if (character.Skills.MainSkillGroup.IsEnabled)
                foreach(var gem in character.Skills.MainSkillGroup.Gems)
                    if (avgList.Contains(gem.Name.ToLower()))
                        showAvg = true;

            return showAvg;
        }

        private double CalculateMaxDPS(float[] comparison)
        {
            double max = 0;

            foreach (var dps in comparison)
                if (dps > max)
                    max = dps;

            return Math.Round(max, 2);
        }

        private double CheckThreshold(double stat)
        {
            return CheckThreshold(stat, 0);
        }

        private double CheckThreshold(double stat, double threshold)
        {
            if (stat >= threshold)
                return stat;

            return 0;
        }

        private bool IsSupport(Character character)
        {
            return character.AuraCount > OutputThresholds.AURA_SUPPORT || character.CurseCount > OutputThresholds.CURSE_SUPPORT;
        }

        private string GenerateTreeURL(string tree)
        {
            var poeURL = "http://poeurl.com/api/?shrink=";
            tree = tree.Trim();
            var param = "{\"url\":\"" + tree + "\"}";
            var val = "http://poeurl.com/";

            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = client.GetAsync(poeURL + System.Web.HttpUtility.UrlEncode(param)).GetAwaiter().GetResult())
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var resp = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        JObject json = JObject.Parse(resp);
                        val = val + json["url"].ToString();
                    }
                }
            }

            return val;
        }

        private bool ShowCharges(string xml)
        {
            var doc = XDocument.Parse(xml);
            var items = doc.Root.Element("Config").Elements("Input");

            foreach (var item in items)
            {
                if (item.Attribute("name").Value == "usePowerCharges" || item.Attribute("name").Value == "useFrenzyCharges" || item.Attribute("name").Value == "useEnduranceCharges")
                    if (item.Attribute("boolean").Value == "true")
                        return true;
            }

            return false;
        }

        private string PrepConfigLine(string abbrev, string value)
        {
            string conf = $"{abbrev}";
            if (value.ToLower() != "true")
                conf += $": {FirstLetterToUpper(value)}";
            return conf;
        }

        public static string FirstLetterToUpper(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        private struct OutputThresholds
        {
            public const double LEECH = 0.1;

            // Basic Defense
            public const double LIFE_FLAT = 1000;
            public const double LIFE_PERCENT = 50;
            public const double LIFE_PERCENT_PER_LEVEL = 1.5;
            public const double LIFE_REGEN = 100;

            public const double ES_FLAT = 300;
            public const double ES_PERCENT = 50;
            public const double ES_PERCENT_PER_LEVEL = 1.5;
            public const double ES_REGEN = 100;

            // Show ele res bigger than the 75 cap
            public const double ELE_RES = 76;
            // Show all positive chaos res
            public const double CHAOS_RES = 0;

            // The amount below specifies the ratio of life to ev/ar: 100 life <> 80+ AR/EV is displayed
            public const double AR_EV_THRESHOLD_PERCENTAGE = 0.8;
            public const double ARMOUR = 5000;
            public const double EVASION = 5000;

            // most shields have 25-30 base, so +10 should be easily doable, spellblock is lower
            public const double BLOCK = 40;
            public const double SPELL_BLOCK = 20;
            // 30 = Acro/Phase Acro, half of that is displayable
            public const double DODGE = 15;
            public const double SPELL_DODGE = 15;

            // Offense
            public const double ACCURACY = 99;
            public const double CRIT_CHANCE = 5;

            // Charges
            public const double CHARGE_MAXIMUM = 3;

            // AURAS AND CURSES
            public const double CURSE_SUPPORT = 3;
            public const double AURA_SUPPORT = 3;
        }

        private EmbedBuilder PrepareEmbed(EmbedType type)
        {
            var embed = new EmbedBuilder();
            embed.WithCurrentTimestamp();
            switch (type)
            {
                case EmbedType.Info:
                    embed.Color = new Color(0, 127, 255);
                    break;

                case EmbedType.Success:
                    embed.Color = new Color(127, 255, 0);
                    break;

                case EmbedType.Warning:
                    embed.Color = new Color(255, 255, 0);
                    break;

                case EmbedType.Error:
                    embed.Color = new Color(255, 127, 0);
                    break;

                default:
                    embed.Color = new Color(255, 255, 255);
                    break;
            }
            return embed;
        }

        private EmbedBuilder PrepareEmbed(Character character, IUser user, EmbedType type)
        {
            var mainSkill = character.Skills.MainSkillGroup;
            var skill = mainSkill.Gems.Where(e => e.Enabled).Select(e => e.Name).First();

            var embed = this.PrepareEmbed(type);
            embed.WithTitle($"{(IsSupport(character) ? "Support" : skill)} - {(character.Ascendancy == string.Empty ? character.Class : character.Ascendancy)} (Lvl: {character.Level})")
                .WithThumbnailUrl($"https://raw.githubusercontent.com/Kyle-Undefined/PoE.Bot/master/Path-Of-Building/Images/Classes/{(character.Ascendancy == string.Empty ? character.Class : character.Ascendancy)}.png")
                .WithAuthor(user)
                .WithCurrentTimestamp();
            return embed;
        }

        private enum EmbedType : uint
        {
            Unknown,
            Success,
            Error,
            Warning,
            Info
        }

        private class Config
        {
            public Config(string var, string abbreviation, string category, string value)
            {
                Var = var;
                Abbreviation = abbreviation;
                Category = category;
                Value = value;
            }

            public string Var { get; }
            public string Abbreviation { get; }
            public string Category { get; }
            public string Value { get; }
        }
    }
}
