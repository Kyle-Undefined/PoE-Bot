// Copied from RougeException/Discord.Net.Commands
// Source - https://github.com/RogueException/Discord.Net/blob/dev/src/Discord.Net.Commands/Readers/TimeSpanTypeReader.cs

namespace PoE.Bot.Parsers
{
	using PoE.Bot.Attributes;
	using Qmmands;
	using System;
	using System.Globalization;
	using System.Threading.Tasks;

	[ConcreteType(typeof(TimeSpan))]
	public class TimeSpanTypeParser : TypeParser<TimeSpan>
	{
		private static readonly string[] Formats = {
			"%d'd'%h'h'%m'm'%s's'", //4d3h2m1s
			"%d'd'%h'h'%m'm'",      //4d3h2m
			"%d'd'%h'h'%s's'",      //4d3h  1s
			"%d'd'%h'h'",           //4d3h
			"%d'd'%m'm'%s's'",      //4d  2m1s
			"%d'd'%m'm'",           //4d  2m
			"%d'd'%s's'",           //4d    1s
			"%d'd'",                //4d
			"%h'h'%m'm'%s's'",      //  3h2m1s
			"%h'h'%m'm'",           //  3h2m
			"%h'h'%s's'",           //  3h  1s
			"%h'h'",                //  3h
			"%m'm'%s's'",           //    2m1s
			"%m'm'",                //    2m
			"%s's'",                //      1s
		};

		public override Task<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
		{
			if(TimeSpan.TryParseExact(value.ToLowerInvariant(), Formats, CultureInfo.InvariantCulture, out var timeSpan))
				return Task.FromResult(new TypeParserResult<TimeSpan>(timeSpan));

			return Task.FromResult(new TypeParserResult<TimeSpan>("TimeSpan can not be parsed."));
		}
	}
}
