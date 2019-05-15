namespace PoE.Bot.Models.PathOfBuilding
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class MinionSummary
	{
		private static readonly Dictionary<string, PropertyInfo> _props = typeof(MinionSummary).GetProperties().ToDictionary(c => c.Name, c => c);

		public MinionSummary(Dictionary<string, string> minionStats)
		{
			foreach (var item in minionStats)
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

		public float AverageDamage { get; private set; }
		public float IgniteDPS { get; private set; }
		public int Life { get; private set; }
		public float LifeLeechGainRate { get; private set; }
		public float LifeRegen { get; private set; }
		public float NetLifeRegen { get; private set; }
		public float Speed { get; private set; }
		public float TotalDot { get; private set; }
		public float TotalDPS { get; private set; }
		public float WithPoisonDPS { get; private set; }
	}
}