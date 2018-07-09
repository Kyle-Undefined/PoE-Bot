namespace PoE.Bot.Modules
{
    using Addons;
    using Addons.Preconditions;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using Objects;
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [Name("Fun Commands"), Ratelimit]
    public class FunModule : BotBase
    {
        [Command("Clap"), Remarks("Replaces spaces in your message with a clap emoji."), Summary("Clap <message>")]
        public Task ClapAsync([Remainder] string message)
            => ReplyAsync(message.Replace(" ", " 👏 "));

        [Command("Enhance"), Remarks("Enhances the Emote into a larger size."), Summary("Enhance <smallEmote>")]
        public async Task EnhanceAsync(string smallEmote)
            => await Context.Message.DeleteAsync().ContinueWith(_ =>
            {
                if (Emote.TryParse(smallEmote, out Emote bigEmote))
                    return ReplyAsync(embed: new EmbedBuilder().WithImageUrl(bigEmote.Url).WithColor(new Color(Context.Random.Next(255), Context.Random.Next(255), Context.Random.Next(255))).Build());
                else if (Regex.Match(smallEmote, @"[^\u0000-\u007F]+", RegexOptions.IgnoreCase).Success)
                    return ReplyAsync(embed: new EmbedBuilder()
                        .WithImageUrl($"https://i.kuro.mu/emoji/256x256/{string.Join("-", IntHelper.GetUnicodeCodePoints(smallEmote).Select(x => x.ToString("X2")))}.png".ToLower())
                        .WithColor(new Color(Context.Random.Next(255), Context.Random.Next(255), Context.Random.Next(255))).Build());
                return ReplyAsync($"{Extras.Cross} I barely recognize myself. *Invalid Emote.*");
            }).ConfigureAwait(false);

        [Command("Expand"), Remarks("Converts text to full width."), Summary("Expand <text>")]
        public Task ExpandAsync([Remainder] string text)
            => ReplyAsync(string.Join(string.Empty, text.Select(x => StringHelper.Normal.Contains(x) ? x : ' ').Select(x => StringHelper.FullWidth[StringHelper.Normal.IndexOf(x)])));

        [Command("GenColor"), Remarks("Generate a color in chat. red, green and blue values can be between 0 and 255."), Summary("GenColor <red> <green> <blue>")]
        public Task GenColorAsync(int red, int green, int blue)
        {
            if (red < 0 || red > 255)
                red = Context.Random.Next(255);
            if (green < 0 || green > 255)
                green = Context.Random.Next(255);
            if (blue < 0 || blue > 255)
                blue = Context.Random.Next(255);
            return ReplyAsync(embed: new EmbedBuilder().WithAuthor(Context.User).WithDescription($"red: `{red}` green: `{green}` blue: `{blue}`").WithColor(new Color(red, green, blue)).Build());
        }

        [Command("Kadaka"), Remarks("Prais our lord and savior, Kadaka!"), Summary("Kadaka")]
        public Task KadakaAsync()
            => ReplyAsync("░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░\n" +
                        "░░░PRAISE░░░░░░▄▀▀█▀▀▄░░░░░░░░░░░░\n" +

                        "░░░OUR░░░░░░░▐░▀░░░▀░▌░░░░░░░░░░░\n" +
                        "░░░LORD░░░░░░▐░█▀░░▀█░▌░░░░░░░░░░\n" +

                        "░░░AND░░░░░░░▐█░░█░░░░▌░░░░░░░░░░\n" +

                        "░░░SAVIOUR░░░░▐▀░░░█░░░▌░░░░░░░░░\n" +
                        "░░░░░░░░░░░░░▐░░█▀▀█░█▌░░░░░░░░░░\n" +
                        "░░░░░░░░░░░░░▐█░░▀▀░░░▌░░░░░░░░░░\n" +
                        "░░░KADAKA!░░░░▐░░█░░░░█▌░░░░░░░░░\n" +
                        "░░░░░░░░░░░░░▐░█░░▀░░░▌░░░░░░░░░░\n" +
                        "░░░░░░░░░░░░░▐█░▀░░█░█▌░░░░░░░░░░");

        [Command("Kuduku"), Remarks("Kuduku has arrived!"), Summary("Kuduku")]
        public Task KudukuAsync()
            => ReplyAsync("░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░░▄▄▄▄▄▄▄▄░░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░▐░░░░░░░░▌░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░▌░▐▄░░▄▌░▐░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░▌░░░░░░░░▐░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░▐░▀▀▄▄▀▀░▌░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░▌░░░▀▀░░░▐░░░░\n" +

                        "░░░░░░░░░░░░░░░░░░░░▌░░░░░░░░▐░░░░\n" +

                        "░░░░Kuduku░░░░░░░░░░░▌░░░░░░░░▐░░░░\n" +

                        "░░░░░░has░░░░░░░░░░░░▌░░░░░░░░▐░░░░\n" +

                        "░░░░░arrived░░░░░░░░░░░▌░░░░░░░░▐░░░░");

        [Command("Mock"), Remarks("Turns text into Spongebob Mocking Meme."), Summary("Mock <text>")]
        public Task MockAsync([Remainder]string text)
        {
            Context.Channel.TriggerTypingAsync();

            string meme = string.Concat(text.ToLower().AsEnumerable().Select((c, i) => i % 2 is 0 ? c : char.ToUpper(c)));
            IEnumerable<string> chunkedMeme = null;
            int charCount = 0;
            int maxChar = 33;

            if (meme.Length > maxChar)
                chunkedMeme = meme.Split(' ', StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxChar).Select(g => string.Join(" ", g));

            string path = @"img/mock.jpg";
            string savePath = @"img/output/stoP-THAt-RiGHT-nOW-" + DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + ".png";

            SKImageInfo info = new SKImageInfo(583, 411);
            using (SKSurface surface = SKSurface.Create(info))
            {
                SKCanvas canvas = surface.Canvas;

                Stream fileStream = File.OpenRead(path);
                canvas.DrawColor(SKColors.White);

                using (SKManagedStream stream = new SKManagedStream(fileStream))
                using (SKBitmap bitmap = SKBitmap.Decode(stream))
                using (SKPaint paint = new SKPaint())
                {
                    SKPaint textPaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        TextAlign = SKTextAlign.Center,
                        TextSize = 32,
                        FakeBoldText = true
                    };

                    canvas.DrawBitmap(bitmap, SKRect.Create(info.Width, info.Height), paint);

                    SKPoint coord = new SKPoint(info.Width / 2, 32);

                    if (meme.Length > maxChar)
                    {
                        foreach (string str in chunkedMeme)
                        {
                            canvas.DrawText(str, coord, textPaint);
                            coord.Offset(0, 42);
                        }
                    }
                    else
                        canvas.DrawText(meme, coord, textPaint);

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (FileStream streamImg = File.OpenWrite(savePath))
                    {
                        data.SaveTo(streamImg);
                    }
                }
            }

            return Context.Channel.SendFileAsync(savePath);
        }

        [Command("Nut"), Remarks("Nut on the chat."), Summary("Nut")]
        public Task NutAsync()
            => ReplyAsync("█▀█ █▄█ ▀█▀");

        [Command("Profile"), Remarks("Shows a users profile."), Summary("Profile [@user]")]
        public Task ProfileAsync(SocketGuildUser user = null)
        {
            user = user ?? Context.User as SocketGuildUser;
            ProfileObject profile = GuildHelper.GetProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id);
            Embed embed = Extras.Embed(Extras.Info)
                .WithAuthor($"{user.Username}'s Profile", user.GetAvatarUrl())
                .WithThumbnailUrl(user.GetAvatarUrl())
                .AddField("Warnings", profile.Warnings, true)
                .AddField("Mod Cases", Context.Server.UserCases.Count(x => x.UserId == user.Id), true)
                .AddField("Tags", Context.Server.Tags.Count(x => x.Owner == user.Id), true)
                .AddField("Shop Items", Context.Server.Shops.Count(x => x.UserId == user.Id), true)
                .Build();
            return ReplyAsync(embed: embed);
        }

        [Command("Rate"), Remarks("Rates something for you out of 10."), Summary("Rate <thingToRate>")]
        public Task RateAsync([Remainder] string thingToRate)
            => ReplyAsync($":thinking: Must I do everything myself? *I would rate '{thingToRate}' a solid {Context.Random.Next(11)}/10*");

        [Command("Toucan"), Remarks("Le Toucan Has Arrive."), Summary("Toucan")]
        public Task ToucanAsync()
            => ReplyAsync("░░░░░░░░▄▄▄▀▀▀▄▄███▄░░░░░░░░░░░░░░\n" +
                        "░░░░░▄▀▀░░░░░░░▐░▀██▌░░░░░░░░░░░░░\n" +
                        "░░░▄▀░░░░▄▄███░▌▀▀░▀█░░░░░░░░░░░░░\n" +
                        "░░▄█░░▄▀▀▒▒▒▒▒▄▐░░░░█▌░░░░░░░░░░░░\n" +
                        "░▐█▀▄▀▄▄▄▄▀▀▀▀▌░░░░░▐█▄░░░░░░░░░░░\n" +
                        "░▌▄▄▀▀░░░░░░░░▌░░░░▄███████▄░░░░░░\n" +
                        "░░░░░░░░░░░░░▐░░░░▐███████████▄░░░\n" +
                        "░░░░░le░░░░░░░▐░░░░▐█████████████▄\n" +
                        "░░░░toucan░░░░░░▀▄░░░▐█████████████▄ \n" +
                        "░░░░░░has░░░░░░░░▀▄▄███████████████ \n" +
                        "░░░░░arrived░░░░░░░░░░░░█▀██████░░");

        [Command("YEEEEAAAHHH", RunMode = RunMode.Async), Alias("Yeah"), Remarks("YEEEEAAAHHH"), Summary("YEEEEAAAHHH")]
        public async Task YeahAsync()
        {
            IUserMessage message = await ReplyAsync("( •_•)");
            await Task.Delay(1000).ConfigureAwait(false);
            await message.ModifyAsync(x => x.Content = "( •_•)>⌐■-■").ConfigureAwait(false);
            await Task.Delay(1200).ConfigureAwait(false);
            await message.ModifyAsync(x => x.Content = "(⌐■_■)\n**YYYYYYEEEEEEEAAAAAHHHHHHH**").ConfigureAwait(false);
        }
    }
}