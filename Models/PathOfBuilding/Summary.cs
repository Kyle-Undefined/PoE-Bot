namespace PoE.Bot.Models.PathOfBuilding
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class Summary
	{
		private static readonly Dictionary<string, PropertyInfo> _props = typeof(Summary).GetProperties().ToDictionary(c => c.Name, c => c);

		public Summary(Dictionary<string, string> playerStats)
		{
			foreach (var item in playerStats)
			{
				if (!_props.TryGetValue(item.Key, out PropertyInfo prop))
					continue;

				object value = null;

				if (prop.PropertyType == typeof(int))
					value = Convert.ToInt32(item.Value);
				else if (prop.PropertyType == typeof(float))
					value = Convert.ToSingle(item.Value);
				else
					value = item.Value;

				if (!(value is null))
					prop.SetValue(this, value);
			}
		}

		public int Armour { get; private set; }
		public float AttackDodgeChance { get; private set; }
		public float AverageDamage { get; private set; }
		public float BleedDPS { get; private set; }
		public float BlockChance { get; private set; }
		public int ChaosResist { get; private set; }
		public int ChaosResistOverCap { get; private set; }
		public int ColdResist { get; private set; }
		public int ColdResistOverCap { get; private set; }
		public float CritChance { get; private set; }
		public float CritMultiplier { get; private set; }
		public int Dex { get; private set; }
		public int EnduranceCharges { get; private set; }
		public int EnduranceChargesMax { get; private set; }
		public int EnergyShield { get; private set; }
		public float EnergyShieldInc { get; private set; }
		public float EnergyShieldLeechGainRate { get; private set; }
		public float EnergyShieldRegen { get; private set; }
		public int Evasion { get; private set; }
		public int FireResist { get; private set; }
		public int FireResistOverCap { get; private set; }
		public int FrenzyCharges { get; private set; }
		public int FrenzyChargesMax { get; private set; }
		public float HitChance { get; private set; }
		public float IgniteDPS { get; private set; }
		public int Int { get; private set; }
		public int Life { get; private set; }
		public float LifeInc { get; private set; }
		public float LifeLeechGainRate { get; private set; }
		public float LifeRegen { get; private set; }
		public int LifeUnreserved { get; private set; }
		public float LifeUnreservedPercent { get; private set; }
		public int LightningResist { get; private set; }
		public int LightningResistOverCap { get; private set; }
		public int Mana { get; private set; }
		public int ManaCost { get; private set; }
		public float ManaInc { get; private set; }
		public float ManaLeechGainRate { get; private set; }
		public float ManaRegen { get; private set; }
		public int ManaUnreserved { get; private set; }
		public float ManaUnreservedPercent { get; private set; }
		public float NetLifeRegen { get; private set; }
		public int PowerCharges { get; private set; }
		public int PowerChargesMax { get; private set; }
		public float Speed { get; private set; }
		public float SpellBlockChance { get; private set; }
		public float SpellDodgeChance { get; private set; }
		public int Str { get; private set; }
		public float TotalDot { get; private set; }
		public float TotalDPS { get; private set; }
		public float WithPoisonAverageDamage { get; private set; }
		public float WithPoisonDPS { get; private set; }
	}
}