namespace PoE.Bot.Attributes
{
	using Microsoft.Extensions.DependencyInjection;
	using System;

	[AttributeUsage(AttributeTargets.Class)]
	public class ServiceAttribute : Attribute
	{
		public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton)
		{
			Lifetime = lifetime;
		}

		public ServiceLifetime Lifetime { get; }
	}
}