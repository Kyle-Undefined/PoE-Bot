namespace PoE.Bot.Helpers
{
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using PoE.Bot.Handlers;
    using PoE.Bot.Handlers.Objects;
    using PoE.Bot.Addons;
    using Drawing = System.Drawing.Color;
    using TwitchLib.Api;

    public class TwitchHelper
    {
        public static async Task BuildAndSend(TwitchObject Stream, SocketGuild Guild, GuildObject Server, ConfigObject Config, DBHandler DB)
        {
            var streamWasLive = Stream.IsLive;

            TwitchAPI TwitchAPI = new TwitchAPI();
            TwitchAPI.Settings.ClientId = Config.APIKeys["TC"];
            TwitchAPI.Settings.AccessToken = Config.APIKeys["TA"];

            var StreamData = await TwitchAPI.Streams.helix.GetStreamsAsync(null, null, 1, null, null, "live", new List<string>(new string[] { Stream.UserId }), null);
            if (!StreamData.Streams.Any()) Stream.IsLive = false;

            if (StreamData.Streams.Any())
            {
                foreach (var stream in StreamData.Streams)
                {
                    if (!Stream.IsLive)
                    {
                        var TwitchUser = await TwitchAPI.Users.helix.GetUsersAsync(new List<string>(new string[] { stream.UserId }));
                        var TwitchGame = await TwitchAPI.Games.helix.GetGamesAsync(new List<string>(new string[] { stream.GameId }));
                        var Embed = Extras.Embed(Drawing.Aqua)
                            .WithTitle(stream.Title)
                            .WithDescription($"\n**{TwitchUser.Users[0].DisplayName}** is playing **{TwitchGame.Games[0].Name}** for {stream.ViewerCount} viewers!\n\n**http://www.twitch.tv/{TwitchUser.Users[0].DisplayName}**")
                            .WithAuthor(TwitchUser.Users[0].DisplayName, TwitchUser.Users[0].ProfileImageUrl, $"http://www.twitch.tv/{TwitchUser.Users[0].DisplayName}")
                            .WithThumbnailUrl(TwitchGame.Games[0].BoxArtUrl.Replace("{width}x{height}", "285x380"))
                            .WithImageUrl(stream.ThumbnailUrl.Replace("{width}x{height}", "640x360"))
                            .Build();

                        var Channel = Guild.GetChannel(Stream.ChannelId) as SocketTextChannel;
                        await Channel.SendMessageAsync(embed: Embed);

                        Stream.IsLive = true;
                        DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
                    }
                }
            }

            if (streamWasLive && !Stream.IsLive) DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
        }
    }
}
