using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Globalization;
using System.IO;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;
using Newtonsoft.Json.Linq;
using TwitchLib.Api;

namespace PoE.Bot.Plugin.Twitch
{
    public class TwitchCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.Twitch Module"; } }
        private static TwitchAPI twitchAPI;

        [Command("addtwitch", "Adds an Twitch stream to a specified channel.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task AddTwitch(CommandContext ctx,
            [ArgumentParameter("Mention of the channel to add the stream to.", true)] ITextChannel channel,
            [ArgumentParameter("Name of the Twitch User.", true)] string user)
        {
            var chf = channel as SocketTextChannel;
            if (chf == null)
                throw new ArgumentException("Invalid channel specified.");
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("No Twitch User specified");

            var a = typeof(PoE.Bot.Core.Client).GetTypeInfo().Assembly;
            var l = Path.GetDirectoryName(a.Location);
            var sp = Path.Combine(l, "config.json");
            var sjson = File.ReadAllText(sp, Encoding.UTF8);
            var sjo = JObject.Parse(sjson);
            var clientID = (string)sjo["twitchClientID"];
            var accessToken = (string)sjo["twitchAccessToken"];

            twitchAPI = new TwitchAPI();
            twitchAPI.Settings.ClientId = clientID;
            twitchAPI.Settings.AccessToken = accessToken;

            var users = await twitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { user }));
            var twitchUser = users.Users[0];

            TwitchPlugin.Instance.AddStream(user, twitchUser.Id, chf.Id);
            var embed = this.PrepareEmbed("Success", "Stream was added successfully.", EmbedType.Success);
            embed.AddField("Details", $"Stream for Twitch User \"{user}\" was added to {chf.Mention}.")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("rmtwitch", "Removes an Twitch stream from a specified channel.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task RemoveTwitch(CommandContext ctx,
            [ArgumentParameter("Mention of the channel to remove the stream from.", true)] ITextChannel channel,
            [ArgumentParameter("Name of the Twitch User.", true)] string user)
        {
            var chf = channel as SocketTextChannel;
            if (chf == null)
                throw new ArgumentException("Invalid channel specified.");
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("No Twitch User specified");

            TwitchPlugin.Instance.RemoveStream(user, chf.Id);
            var embed = this.PrepareEmbed("Success", "Stream was removed successfully.", EmbedType.Success);
            embed.AddField("Details", $"Stream for Twitch User \"{user}\" was removed from {chf.Mention}.")
               .WithAuthor(ctx.User)
               .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("listtwitch", "Lists Twitch streams active in the current guild.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task ListTwitch(CommandContext ctx)
        {
            var gld = ctx.Guild as SocketGuild;
            var streams = TwitchPlugin.Instance.GetStreams(gld.Channels.Select(xch => xch.Id).ToArray());

            var sb = new StringBuilder();
            foreach (var stream in streams)
            {
                var xch = gld.GetChannel(stream.ChannelId) as SocketTextChannel;
                sb.Append($"```Name: {stream.Name}\nChannel: #{xch.Name}```");
                sb.AppendLine("---------");
            }

            var embedChunks = ChunkString(sb.ToString(), 1024);
            foreach (var chunk in embedChunks)
            {
                var chunkedEmbed = this.PrepareEmbed(EmbedType.Info);
                chunkedEmbed.AddField("Twitch Streams", chunk);
                await ctx.Channel.SendMessageAsync("", false, chunkedEmbed.Build());
            }
        }

        [Command("testtwitch", "Tests the Twitch feeds active in the current guild.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task TestTwitch(CommandContext ctx)
        {
            var gld = ctx.Guild as SocketGuild;
            var streams = TwitchPlugin.Instance.GetStreams(gld.Channels.Select(xch => xch.Id).ToArray());
            List<Twitch> twitches = streams.ToList();
            var a = typeof(PoE.Bot.Core.Client).GetTypeInfo().Assembly;
            var l = Path.GetDirectoryName(a.Location);
            var sp = Path.Combine(l, "config.json");
            var sjson = File.ReadAllText(sp, Encoding.UTF8);
            var sjo = JObject.Parse(sjson);
            var clientID = (string)sjo["twitchClientID"];
            var accessToken = (string)sjo["twitchAccessToken"];

            twitchAPI = new TwitchAPI();
            twitchAPI.Settings.ClientId = clientID;
            twitchAPI.Settings.AccessToken = accessToken;

            var streamData = await twitchAPI.Streams.helix.GetStreamsAsync(null, null, 100, null, null, "live", twitches.Select(x => x.UserId).ToList(), null);
            foreach(var stream in streamData.Streams)
            {
                var twitchUser = await twitchAPI.Users.helix.GetUsersAsync(new List<string>(new string[] { stream.UserId }));
                var twitchGame = await twitchAPI.Games.helix.GetGamesAsync(new List<string>(new string[] { stream.GameId }));
                var embed = this.PrepareEmbed(EmbedType.Info);
                embed.WithTitle(stream.Title)
                    .WithDescription($"\n**{twitchUser.Users[0].DisplayName}** is playing **{twitchGame.Games[0].Name}** for {stream.ViewerCount} viewers!\n\n**http://www.twitch.tv/{twitchUser.Users[0].DisplayName}**")
                    .WithAuthor(twitchUser.Users[0].DisplayName, twitchUser.Users[0].ProfileImageUrl, $"http://www.twitch.tv/{twitchUser.Users[0].DisplayName}")
                    .WithThumbnailUrl(twitchGame.Games[0].BoxArtUrl.Replace("{width}x{height}", "285x380"))
                    .WithImageUrl(stream.ThumbnailUrl.Replace("{width}x{height}", "640x360"));

                var channel = twitches.Find(x => x.UserId == stream.UserId).ChannelId;
                IMessageChannel chan = gld.GetChannel(channel) as IMessageChannel;
                await chan.SendMessageAsync("", false, embed.Build());

                await Task.Delay(15000);
            }
        }

        private EmbedBuilder PrepareEmbed(EmbedType type)
        {
            var embed = new EmbedBuilder();
            embed.WithCurrentTimestamp();
            switch (type)
            {
                case EmbedType.Info:
                    embed.Color = new Color(0, 127, 255);
                    break;

                case EmbedType.Success:
                    embed.Color = new Color(127, 255, 0);
                    break;

                case EmbedType.Warning:
                    embed.Color = new Color(255, 255, 0);
                    break;

                case EmbedType.Error:
                    embed.Color = new Color(255, 127, 0);
                    break;

                default:
                    embed.Color = new Color(255, 255, 255);
                    break;
            }
            return embed;
        }

        private EmbedBuilder PrepareEmbed(string title, string desc, EmbedType type)
        {
            var embed = this.PrepareEmbed(type);
            embed.Title = title;
            embed.Description = desc;
            embed.WithCurrentTimestamp();
            return embed;
        }

        private enum EmbedType : uint
        {
            Unknown,
            Success,
            Error,
            Warning,
            Info
        }

        static IEnumerable<string> ChunkString(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}
