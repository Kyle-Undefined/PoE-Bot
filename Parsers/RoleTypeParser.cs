// Copied from RougeException/Discord.Net.Commands
// Source - https://github.com/RogueException/Discord.Net/blob/dev/src/Discord.Net.Commands/Readers/RoleTypeReader.cs

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

	[ConcreteType(typeof(SocketRole))]
	public class DiscordRoleTypeParser<T> : TypeParser<T> where T : class, IRole
	{
		public override Task<TypeParserResult<T>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
		{
			var _context = context as GuildContext;
			var results = new Dictionary<ulong, GenericParseResult<T>>();
			var roles = _context.Guild.Roles;

			//By Mention (1.0)
			if (MentionUtils.TryParseRole(value, out ulong id))
				AddResult(results, _context.Guild.GetRole(id) as T, 1.00f);

			//By Id (0.9)
			if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
				AddResult(results, _context.Guild.GetRole(id) as T, 0.90f);

			//By Name (0.7-0.8)
			foreach (var role in roles.Where(x => string.Equals(value, x.Name, StringComparison.OrdinalIgnoreCase)))
				AddResult(results, role as T, role.Name == value ? 0.80f : 0.70f);

			if (results.Count > 0)
				return Task.FromResult(new TypeParserResult<T>(results.Values.OrderBy(a => a.Score).FirstOrDefault()?.Value));

			return Task.FromResult(new TypeParserResult<T>("Role not found."));
		}

		private void AddResult(Dictionary<ulong, GenericParseResult<T>> results, T role, float score)
		{
			if (role != null && !results.ContainsKey(role.Id))
			{
				results.Add(role.Id, new GenericParseResult<T>
				{
					Score = score,
					Value = role
				});
			}
		}
	}
}
