namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Handlers.Objects;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;

    [Name("Tag Commands"), Group("Tag"), Ratelimit]
    public class TagModule : Base
    {
        [Command, Priority(0), Summary("Executes a tag with the given name.")]
        public Task TagAsync(string Name)
        {
            if (!Exists(Name, true)) return Task.CompletedTask;
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == Name);
            Tag.Uses++;
            return ReplyAsync(Tag.Content, Save: 's');
        }

        [Command("Create", RunMode = RunMode.Async), Alias("new", "Make"), Priority(10), Remarks("Initiates Tag Creation wizard."), Summary("Tag Create")]
        public async Task CreateAsync()
        {
            var Name = Context.GuildHelper.CalculateResponse(await WaitAsync($"What would be the name of your tag? (c to cancel)"));
            if (!Name.Item1) { await ReplyAsync(Name.Item2); return; }
            Name.Item2 = Name.Item2.Replace(" ", "-");
            if (Exists(Name.Item2)) return;
            else if (NameCheck(Name.Item2))
            {
                await ReplyAsync($"`{Name.Item2}` is a reserved name. Try something else?");
                return;
            }
            var Content = Context.GuildHelper.CalculateResponse(await WaitAsync($"What would be the content of your tag? (c to cancel)",
                Timeout: TimeSpan.FromMinutes(2)));
            if (!Content.Item1) { await ReplyAsync(Content.Item2); return; }
            Context.Server.Tags.Add(new TagObject
            {
                Uses = 0,
                Name = Name.Item2,
                Owner = Context.User.Id,
                Content = Content.Item2,
                CreationDate = DateTime.UtcNow
            });
            await ReplyAsync($"Tag `{Name.Item2}` has been created {Extras.OkHand}", Save: 's');
        }

        [Command("Update"), Alias("Modify", "Change"), Priority(10), Remarks("Updates an existing tag"), Summary("Tag Update <name> <content>")]
        public async Task UpdateAsync(string Name, [Remainder] string Content)
        {
            if (!Exists(Name)) return;
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == Name);
            if (Context.User.Id != Tag.Owner) { await ReplyAsync($"You aren't the owner of `{Name}` {Extras.Cross}"); return; }
            Tag.Content = Content;
            await ReplyAsync($"Tag `{Name}`'s contant has been updated.", Save: 's');
        }

        [Command("Remove"), Alias("Delete"), Priority(10), Remarks("Deletes a tag."), Summary("Tag Delete <Name>")]
        public Task RemoveAsync(string Name)
        {
            if (!Exists(Name)) return Task.CompletedTask;
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == Name);
            if (Context.User.Id != Tag.Owner) return ReplyAsync($"You aren't the owner of `{Name}` {Extras.Cross}");
            Context.Server.Tags.Remove(Tag);
            return ReplyAsync($"Tag `{Name}` has been deleted {Extras.OkHand}", Save: 's');
        }

        [Command("User"), Priority(10), Remarks("Shows all tags owned by you or a given user."), Summary("Tag User [@User]")]
        public Task UserAsync(IGuildUser User = null)
        {
            User = User ?? Context.User as IGuildUser;
            var UserTag = Context.Server.Tags.Where(x => x.Owner == User.Id).Select(x => x.Name);
            if (!Context.Server.Tags.Any() || !UserTag.Any()) return ReplyAsync($"`{User}` doesn't have any tags.");
            return PagedReplyAsync(Context.GuildHelper.Pages(UserTag), $"{User.Username} Tag Collection");
        }

        [Command("Claim"), Priority(10), Remarks("Claims a tag whose owner isn't in server anymore."), Summary("Tag Claim <Name>")]
        public Task ClaimAsync(string Name)
        {
            if (!Exists(Name)) return Task.CompletedTask;
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == Name);
            if ((Context.Guild as SocketGuild).Users.Where(x => x.Id == Tag.Owner).Any())
                return ReplyAsync($"Tag owner is still in this guild.");
            Tag.Owner = Context.User.Id;
            return ReplyAsync($"You are now the owner of `{Tag.Name}` tag {Extras.OkHand}", Save: 's');
        }

        [Command("Info"), Priority(10), Remarks("Displays information about a given tag."), Summary("Tag Info <Name>")]
        public async Task InfoAsync(string Name)
        {
            if (!Exists(Name, true)) return;
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == Name);
            var User = StringHelper.ValidateUser(Context.Guild, Tag.Owner);
            await ReplyAsync(string.Empty, Extras.Embed(Drawing.Aqua)
                .WithAuthor($"Tag Information", Context.Client.CurrentUser.GetAvatarUrl())
                .AddField("Name", Tag.Name, true)
                .AddField("Owner", User, true)
                .AddField("Uses", Tag.Uses, true)
                .AddField("Created On", Tag.CreationDate, true)
                .AddField("Content", Tag.Content, false)
                .WithThumbnailUrl("https://png.icons8.com/nolan/80/000000/tags.png")
                .Build());
        }

        bool Exists(string Name, bool Suggest = false)
        {
            if (Context.Server.Tags.FirstOrDefault(x => x.Name == Name) == null)
            {
                if (Suggest)
                {
                    var Suggested = Context.Server.Tags.Where(
                        x => x.Name.ToLower().Contains(Name.ToLower())).Select(
                        x => x.Name).Take(5);
                    _ = ReplyAsync($"Couldn't find tag `{Name}` {Extras.Cross} " +
                       (Suggested.Any() ? $"Try these: {string.Join(", ", Suggested)}" : null));
                }
                return false;
            }
            return true;
        }

        bool NameCheck(string Name)
            => new[] { "help", "about", "tag", "delete", "remove", "delete", "info", "modify", "update", "user", "list" }.Any(x
                => Name.ToLower().Contains(x));
    }
}
