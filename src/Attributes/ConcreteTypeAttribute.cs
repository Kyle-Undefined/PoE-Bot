namespace PoE.Bot.Attributes
{
	using System;

	[AttributeUsage(AttributeTargets.Class)]
	public class ConcreteTypeAttribute : Attribute
	{
		public ConcreteTypeAttribute(params Type[] types) => Types = types;

		public Type[] Types { get; }
	}
}
