namespace PoE.Bot.Attributes
{
	using System;

	[AttributeUsage(AttributeTargets.Method)]
	public class UsageAttribute : Attribute
	{
		public UsageAttribute(string exampleUsage) => ExampleUsage = exampleUsage;

		public string ExampleUsage { get; }
	}
}