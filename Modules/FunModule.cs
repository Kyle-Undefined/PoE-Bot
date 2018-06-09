namespace PoE.Bot.Modules
{
    using System;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;
    using System.Collections.Generic;
    using SkiaSharp;

    [Name("Fun Commands"), Ratelimit]
    public class FunModule : BotBase
    {
        [Command("Clap"), Remarks("Replaces spaces in your message with a clap emoji."), Summary("Clap <Message>")]
        public Task ClapAsync([Remainder] string Message) => ReplyAsync(Message.Replace(" ", " 👏 "));

        [Command("Rate"), Remarks("Rates something for you out of 10."), Summary("Rate <ThingToRate>")]
        public Task RateAsync([Remainder] string ThingToRate) => ReplyAsync($":thinking: Must I do everything myself? *I would rate '{ThingToRate}' a solid {Context.Random.Next(11)}/10*");

        [Command("Expand"), Remarks("Converts text to full width."), Summary("Expand <Text>")]
        public Task ExpandAsync([Remainder] string Text)
            => ReplyAsync(string.Join("", Text.Select(x => StringHelper.Normal.Contains(x) ? x : ' ').Select(x => StringHelper.FullWidth[StringHelper.Normal.IndexOf(x)])));

        [Command("Profile"), Remarks("Shows a users profile."), Summary("Profile [@User]")]
        public Task ProfileAsync(SocketGuildUser User = null)
        {
            User = User ?? Context.User as SocketGuildUser;
            var Profile = Context.GuildHelper.GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor($"{User.Username}'s Profile", User.GetAvatarUrl())
                .WithThumbnailUrl(User.GetAvatarUrl())
                .AddField("Warnings", Profile.Warnings, true)
                .AddField("Mod Cases", Context.Server.UserCases.Where(x => x.UserId == User.Id).Count(), true)
                .AddField("Tags", Context.Server.Tags.Where(x => x.Owner == User.Id).Count(), true)
                .AddField("Shop Items", Context.Server.Shops.Where(x => x.UserId == User.Id).Count(), true)
                .Build();
            return ReplyAsync(Embed: Embed);
        }

        [Command("Mock"), Remarks("Turns text into Spongebob Mocking Meme."), Summary("Mock <Text>")]
        public Task MockAsync([Remainder]string Text)
        {
            Context.Channel.TriggerTypingAsync();

            var meme = string.Concat(Text.ToLower().AsEnumerable().Select((c, i) => i % 2 is 0 ? c : char.ToUpper(c)));
            IEnumerable<string> chunkedMeme = null;
            var charCount = 0;
            var maxChar = 33;

            if (meme.Length > maxChar)
                chunkedMeme = meme.Split(' ', StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxChar).Select(g => string.Join(" ", g));

            string path = @"img/mock.jpg";
            string savePath = @"img/output/stoP-THAt-RiGHT-nOW-" + DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + ".png";

            var info = new SKImageInfo(583, 411);
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;

                Stream fileStream = File.OpenRead(path);
                canvas.DrawColor(SKColors.White);

                using (var stream = new SKManagedStream(fileStream))
                using (var bitmap = SKBitmap.Decode(stream))
                using (var paint = new SKPaint())
                {
                    var textPaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        TextAlign = SKTextAlign.Center,
                        TextSize = 32,
                        FakeBoldText = true
                    };

                    canvas.DrawBitmap(bitmap, SKRect.Create(info.Width, info.Height), paint);

                    var coord = new SKPoint(info.Width / 2, 32);

                    if (meme.Length > maxChar)
                    {
                        foreach (var str in chunkedMeme)
                        {
                            canvas.DrawText(str, coord, textPaint);
                            coord.Offset(0, 42);
                        }
                    }
                    else
                        canvas.DrawText(meme, coord, textPaint);

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var streamImg = File.OpenWrite(savePath))
                    {
                        data.SaveTo(streamImg);
                    }
                }
            }

            return Context.Channel.SendFileAsync(savePath);
        }

        [Command("Enhance"), Remarks("Enhances the Emote into a larger size."), Summary("Enhance <SmallEmote>")]
        public Task EnhanceAsync(string SmallEmote)
        {
            Random Rand = new Random();
            if (Emote.TryParse(SmallEmote, out var BigEmote))
                return ReplyAsync(embed: new EmbedBuilder().WithImageUrl(BigEmote.Url).WithColor(new Color(Rand.Next(0, 256), Rand.Next(0, 256), Rand.Next(0, 256))).Build());
            else if (Regex.Match(SmallEmote, @"[^\u0000-\u007F]+", RegexOptions.IgnoreCase).Success)
                return ReplyAsync(embed: new EmbedBuilder().WithImageUrl($"https://twemoji.maxcdn.com/2/72x72/{GetUnicodeCodePoints(SmallEmote).FirstOrDefault():X4}.png".ToLower())
                    .WithColor(new Color(Rand.Next(0, 256), Rand.Next(0, 256), Rand.Next(0, 256))).Build());
            return ReplyAsync($"{Extras.Cross} I barely recognize myself. *Invalid Emote.*");
        }

        [Command("Nut"), Remarks("Nut on the chat."), Summary("Nut")]
        public Task NutAsync()
            => ReplyAsync("█▀█ █▄█ ▀█▀");

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

        [Command("YEEEEAAAHHH"), Alias("Yeah"), Remarks(""), Summary("YEEEEAAAHHH")]
        public async Task YeahAsync()
        {
            var Message = await ReplyAsync("( •_•)");
            await Task.Delay(1000);
            await Message.ModifyAsync(x => x.Content = "( •_•)>⌐■-■");
            await Task.Delay(1200);
            await Message.ModifyAsync(x => x.Content = "(⌐■_■)\n**YYYYYYEEEEEEEAAAAAHHHHHHH**");
        }

        public IEnumerable<int> GetUnicodeCodePoints(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                int unicodeCodePoint = char.ConvertToUtf32(s, i);
                if (unicodeCodePoint > 0xffff)
                    i++;
                yield return unicodeCodePoint;
            }
        }
    }
}
