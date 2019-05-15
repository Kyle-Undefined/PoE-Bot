namespace PoE.Bot.Extensions
{
	using PoE.Bot.Attributes;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Reflection;

	public static class CommandExtension
	{
		public static CommandService AddTypeParsers(this CommandService commands, Assembly assembly)
		{
			var typeParserInterface = Array.Find(commands.GetType().Assembly.GetTypes(), x => x.Name == "ITypeParser");
			var addParser = commands.GetType().GetMethod("AddParserInternal", BindingFlags.NonPublic | BindingFlags.Instance);

			foreach(var parser in assembly.GetTypes().Where(x => typeParserInterface.IsAssignableFrom(x) && x.IsDefined(typeof(ConcreteTypeAttribute))))
			{
				var attribute = parser.GetCustomAttributes<ConcreteTypeAttribute>(false).First();

				foreach (var type in attribute.Types)
				{
					var constructed = parser.IsGenericType ? parser.MakeGenericType(type) : parser;

					addParser.Invoke(commands, new[]
					{
						constructed.BaseType.GetGenericArguments().First(),
						Activator.CreateInstance(constructed),
						true
					});
				}
			}

			return commands;
		}
	}
}
