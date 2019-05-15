namespace PoE.Bot.Extensions
{
	using Discord;
	using Discord.Rest;
	using Discord.WebSocket;
	using System.Threading.Tasks;

	public static class MessageExtension
	{
		public static Task<RestUserMessage> SendMessageAsync(this SocketTextChannel channel, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null) =>
			channel.SendMessageAsync(text, isTTS, embed, options);
	}
}