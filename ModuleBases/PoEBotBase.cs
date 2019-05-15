namespace PoE.Bot.ModuleBases
{
	using Discord;
	using PoE.Bot.Addons.Interactive;
	using PoE.Bot.Contexts;
	using System.Threading.Tasks;

	public class PoEBotBase : InteractiveBase<GuildContext>
	{
		public Task<IUserMessage> PagedReplyAsync(PaginatedMessage paginatedMessage) => base.PagedReplyAsync(paginatedMessage);

		public Task<IUserMessage> ReplyAsync(string message = null, Embed embed = null) => base.ReplyAsync(message, embed: embed);

		public Task<IUserMessage> ReplyWithEmoteAsync(IEmote emote) => ReplyAsync(emote.Name);
	}
}