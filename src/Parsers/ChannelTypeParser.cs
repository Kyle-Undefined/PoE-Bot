// Copied from RougeException/Discord.Net.Commands
// Source - https://github.com/RogueException/Discord.Net/blob/dev/src/Discord.Net.Commands/Readers/ChannelTypeReader.cs

namespace PoE.Bot.Parsers
{
	using Discord;
	using Discord.WebSocket;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading.Tasks;

	[ConcreteType(typeof(SocketGuildChannel), typeof(SocketTextChannel))]
	public class DiscordChannelTypeParser<T> : TypeParser<T> where T : class, IGuildChannel
	{
		public override Task<TypeParserResult<T>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
		{
			var _context = context as GuildContext;
			var results = new Dictionary<ulong, GenericParseResult<T>>();
			var channels = _context.Guild.Channels;

			//By Mention (1.0)
			if (MentionUtils.TryParseChannel(value, out ulong id))
				AddResult(results, _context.Guild.GetChannel(id) as T, 1.00f);

			//By Id (0.9)
			if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
				AddResult(results, _context.Guild.GetChannel(id) as T, 0.90f);

			//By Name (0.7-0.8)
			foreach (var channel in channels.Where(x => string.Equals(value, x.Name, StringComparison.OrdinalIgnoreCase)))
				AddResult(results, channel as T, channel.Name == value ? 0.80f : 0.70f);

			if (results.Count > 0)
				return Task.FromResult(new TypeParserResult<T>(results.Values.OrderBy(a => a.Score).FirstOrDefault()?.Value));

			return Task.FromResult(new TypeParserResult<T>("Channel not found."));
		}

		private void AddResult(Dictionary<ulong, GenericParseResult<T>> results, T channel, float score)
		{
			if (channel != null && !results.ContainsKey(channel.Id))
			{
				results.Add(channel.Id, new GenericParseResult<T>
				{
					Score = score,
					Value = channel
				});
			}
		}
	}
}
