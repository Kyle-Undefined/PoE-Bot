// Copied from RougeException/Discord.Net.Commands
// Source - https://github.com/RogueException/Discord.Net/blob/dev/src/Discord.Net.Commands/Readers/UserTypeReader.cs

namespace PoE.Bot.Parsers
{
	using Discord;
	using Discord.WebSocket;
	using PoE.Bot.Contexts;
	using PoE.Bot.Attributes;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.Linq;
	using System.Threading.Tasks;

	[ConcreteType(typeof(SocketGuildUser), typeof(SocketUser))]
	public class DiscordUserTypeParser<T> : TypeParser<T> where T : class, IUser
	{
		public override async Task<TypeParserResult<T>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
		{
			var _context = context as GuildContext;
			var results = new Dictionary<ulong, GenericParseResult<T>>();
			var channelUsers = (_context.Channel as ISocketMessageChannel)?.GetUsersAsync(CacheMode.CacheOnly).Flatten();
			IReadOnlyCollection<IGuildUser> guildUsers = ImmutableArray.Create<IGuildUser>();

			if (_context.Guild != null)
				guildUsers = await ((IGuild)_context.Guild).GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);

			//By Mention (1.0)
			if (MentionUtils.TryParseUser(value, out var id))
			{
				if (_context.Guild != null)
					AddResult(results, await (_context.Guild as IGuild).GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 1.00f);
				else
					AddResult(results, await (_context.Channel as ISocketMessageChannel).GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 1.00f);
			}

			//By Id (0.9)
			if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
			{
				if (_context.Guild != null)
					AddResult(results, await (_context.Guild as IGuild).GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 0.90f);
				else
					AddResult(results, await (_context.Channel as ISocketMessageChannel).GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 0.90f);
			}

			var index = value.LastIndexOf('#');
			if (index >= 0)
			{
				var username = value.Substring(0, index);

				if (ushort.TryParse(value.Substring(index + 1), out var discriminator))
				{
					var channelUser = await channelUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator && string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
					AddResult(results, channelUser as T, channelUser?.Username == username ? 0.85f : 0.75f);

					var guildUser = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator && string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
					AddResult(results, guildUser as T, guildUser?.Username == username ? 0.80f : 0.70f);
				}
			}

			{
				await channelUsers.Where(x => string.Equals(value, x.Username, StringComparison.OrdinalIgnoreCase))
					.ForEachAsync(channelUser => AddResult(results, channelUser as T, channelUser.Username == value ? 0.65f : 0.55f)) .ConfigureAwait(false);

				foreach (var guildUser in guildUsers.Where(x => string.Equals(value, x.Username, StringComparison.OrdinalIgnoreCase)))
					AddResult(results, guildUser as T, guildUser.Username == value ? 0.60f : 0.50f);
			}

			{
				await channelUsers.Where(x => string.Equals(value, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase))
					.ForEachAsync(channelUser => AddResult(results, channelUser as T, (channelUser as IGuildUser)?.Nickname == value ? 0.65f : 0.55f)).ConfigureAwait(false);

				foreach (var guildUser in guildUsers.Where(x => string.Equals(value, x.Nickname, StringComparison.OrdinalIgnoreCase)))
					AddResult(results, guildUser as T, guildUser.Nickname == value ? 0.60f : 0.50f);
			}

			if (results.Count > 0)
				return new TypeParserResult<T>(results.Values.OrderByDescending(a => a.Score).First().Value);

			return new TypeParserResult<T>("User not found.");
		}

		private static void AddResult(IDictionary<ulong, GenericParseResult<T>> results, T user, float score)
		{
			if (user != null && !results.ContainsKey(user.Id))
			{
				results.Add(user.Id, new GenericParseResult<T>
				{
					Score = score,
					Value = user
				});
			}
		}

	}
}
