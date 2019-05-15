namespace PoE.Bot.Extensions
{
	using Microsoft.Extensions.DependencyInjection;
	using PoE.Bot.Attributes;
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	public static class CollectionExtension
	{
		public static IServiceCollection AddServices(this IServiceCollection services, IEnumerable<Type> types)
		{
			foreach (var type in types)
			{
				switch (type.GetCustomAttribute<ServiceAttribute>().Lifetime)
				{
					case ServiceLifetime.Scoped:
						services.AddScoped(type);
						break;

					case ServiceLifetime.Singleton:
						services.AddSingleton(type);
						break;

					case ServiceLifetime.Transient:
						services.AddTransient(type);
						break;
				}
			}

			return services;
		}

		public static List<List<T>> SplitList<T>(this List<T> me, int size = 5)
		{
			var list = new List<List<T>>();
			for (int i = 0; i < me.Count; i += size)
				list.Add(me.GetRange(i, Math.Min(size, me.Count - i)));
			return list;
		}
	}
}