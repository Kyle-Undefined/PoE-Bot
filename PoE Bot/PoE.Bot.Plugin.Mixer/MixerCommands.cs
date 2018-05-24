using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Plugin.Mixer
{
    public class MixerCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.Mixer Module"; } }

        [Command("addmixer", "Adds a Mixer stream to a specified channel.", CheckerId = "CoreModerator", CheckPermissions = true)]
        public async Task AddMixer(CommandContext ctx,
            [ArgumentParameter("Mention of the channel to add the stream to.", true)] ITextChannel channel,
            [ArgumentParameter("Name of the Mixer User.", true)] string user)
        {
            var chf = channel as SocketTextChannel;
            if (chf == null)
                throw new ArgumentException("Invalid channel specified.");
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("No Mixer User specified.");

            MixerAPI mixer = new MixerAPI();
            uint userID = await mixer.GetUserId(user);
            uint chanID = await mixer.GetChannelId(user);

            if (userID == 0)
                throw new HttpRequestException("No Mixer User was found.");
            if (chanID == 0)
                throw new HttpRequestException("No Mixer Channel was found.");

            MixerPlugin.Instance.AddStream(user, userID, chanID, chf.Id);
            var embed = this.PrepareEmbed("Success", "Stream was added successfully.", EmbedType.Success);
            embed.AddField("Details", $"Stream for Mixer User \"{user}\" was added to {chf.Mention}.")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("rmmixer", "Removes a Mixer stream from a specified channel.", CheckerId = "CoreModerator", CheckPermissions = true)]
        public async Task RemoveMixer(CommandContext ctx,
            [ArgumentParameter("Mention of the channel to remove the stream from.", true)] ITextChannel channel,
            [ArgumentParameter("Name of the Mixer User.", true)] string user)
        {
            var chf = channel as SocketTextChannel;
            if (chf == null)
                throw new ArgumentException("Invalid channel specified.");
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException("No Mixer User specified.");

            MixerPlugin.Instance.RemoveStream(user, chf.Id);
            var embed = this.PrepareEmbed("Success", "Stream was removed successfully.", EmbedType.Success);
            embed.AddField("Details", $"Stream for Mixer User \"{user}\" was removed from {chf.Mention}.")
               .WithAuthor(ctx.User)
               .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("listmixer", "Lists Mixer streams active in the current guild.", CheckerId = "CoreModerator", CheckPermissions = true)]
        public async Task ListMixer(CommandContext ctx)
        {
            var gld = ctx.Guild as SocketGuild;
            var streams = MixerPlugin.Instance.GetStreams(gld.Channels.Select(xch => xch.Id).ToArray());

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
                chunkedEmbed.AddField("Mixer Streams", chunk);
                await ctx.Channel.SendMessageAsync("", false, chunkedEmbed.Build());
            }
        }

        [Command("testmixer", "Tests the Mixer feeds active in the current guild.", CheckerId = "CoreModerator", CheckPermissions = true)]
        public async Task TestMixer(CommandContext ctx)
        {
            var gld = ctx.Guild as SocketGuild;
            var streams = MixerPlugin.Instance.GetStreams(gld.Channels.Select(xch => xch.Id).ToArray());

            foreach (var stream in streams)
            {
                MixerAPI mixer = new MixerAPI();
                string chanJson = await mixer.GetChannel(stream.MixerChannelId);
                bool chanIsLive = mixer.IsChannelLive(chanJson);

                if (!chanIsLive)
                    stream.IsLive = false;

                if(chanIsLive && !stream.IsLive)
                {
                    var embed = new EmbedBuilder();
                    embed.WithTitle(mixer.GetChannelTitle(chanJson))
                        .WithDescription($"\n**{stream.Name}** is playing **{mixer.GetChannelGame(chanJson)}** for {mixer.GetViewerCount(chanJson).ToString()} viewers!\n\n**https://mixer.com/{stream.Name}**")
                        .WithAuthor(stream.Name, mixer.GetUserAvatar(stream.UserId), $"https://mixer.com/{stream.Name}")
                        .WithThumbnailUrl(mixer.GetChannelGameCover(chanJson))
                        .WithImageUrl(mixer.GetChannelThumbnail(chanJson))
                        .WithColor(new Color(0, 127, 255));

                    PoE_Bot.Client.SendEmbed(embed, stream.ChannelId);
                    await Task.Delay(15000);
                }
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
