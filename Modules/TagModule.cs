namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Text;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;

    [Name("Tag Commands"), Group("Tag"), Ratelimit]
    public class TagModule : BotBase
    {
        [Command, Priority(0), Summary("Gets a tag with the given name.")]
        public Task TagAsync([Remainder] string TagName)
        {
            if (!(Context.Server.Tags.Any(t => t.Name == TagName.ToLower())))
                return SuggestTagsAsync(TagName.ToLower());
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == TagName.ToLower());
            Tag.Uses++;
            return ReplyAsync(Tag.Content, Save: 's');
        }

        [Command("List"), Remarks("Shows all tags in a list."), Summary("Tag List"), Priority(1)]
        public Task ListAsync()
            => PagedReplyAsync(Context.GuildHelper.Pages(Context.Server.Tags.Select(t => t.Name)), $"Tag Collection");

        [Command("Search"), Remarks("Shows all tags that match the search."), Summary("Tag Search <TagName>"), Priority(1)]
        public Task SearchAsync([Remainder] string TagName)
        {
            var LDTags = Context.Server.Tags.Where(x => LevenshteinDistance(TagName.ToLower(), x.Name) < 5);
            var ContainTags = Context.Server.Tags.Where(x => x.Name.Contains(TagName.ToLower())).Take(5);
            var Searched = LDTags.Union(ContainTags).Select(t => t.Name);
            return PagedReplyAsync(Context.GuildHelper.Pages(Searched), $"Tag Search Results");
        }

        [Command("Create"), Remarks("Creates a tag with the specified content."), Summary("Tag Create <TagName: To specify more than one word, wrap your name with quotes \"like this\"> <TagContent>"), Priority(1)]
        public Task CreateAsync(string TagName, [Remainder] string TagContent)
        {
            if (Context.Server.Tags.Any(t => t.Name == TagName.ToLower()))
                return ReplyAsync($"{Extras.Cross} You chose the road, old friend. God put me at the end of it. *There is already a tag with this name.*");
            Context.Server.Tags.Add(new TagObject
            {
                Uses = 0,
                Name = TagName.ToLower(),
                Owner = Context.User.Id,
                Content = TagContent,
                CreationDate = DateTime.Now
            });
            return ReplyAsync($"I think that will come in handy at some point. *Tag `{TagName}` has been created.* {Extras.OkHand}", Save: 's');
        }

        [Command("Update"), Alias("Modify", "Change"), Remarks("Updates an existing tag"), Summary("Tag Update <TagName: To specify more than one word, wrap your name with quotes \"like this\"> <TagContent>"), Priority(1)]
        public Task UpdateAsync(string TagName, [Remainder] string TagContent)
        {
            if (!(Context.Server.Tags.Any(t => t.Name == TagName.ToLower())))
                return ReplyAsync($"{Extras.Cross} Return to the dirt! *There is no tag with this name.*");
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == TagName);
            if (Context.User.Id != Tag.Owner)
                return ReplyAsync($"{Extras.Cross} I have a hunch I'll be needing this. *You aren't the owner of `{TagName}`*");
            Tag.Content = TagContent;
            return ReplyAsync($"Thank you, my ancestors. I will repay your gift. *Tag `{TagName}`'s contant has been updated.* {Extras.OkHand}", Save: 's');
        }

        [Command("Remove"), Alias("Delete"), Remarks("Deletes a tag."), Summary("Tag Delete <TagName>"), Priority(1)]
        public Task RemoveAsync([Remainder] string TagName)
        {
            if (!(Context.Server.Tags.Any(t => t.Name == TagName.ToLower())))
                return ReplyAsync($"{Extras.Cross} It was possible to be happy here once. *There is no tag with this name.*");
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == TagName);
            if (Context.User.Id != Tag.Owner)
                return ReplyAsync($"{Extras.Cross} This item whispers of destiny. *You aren't the owner of `{TagName}`*");
            Context.Server.Tags.Remove(Tag);
            return ReplyAsync($"I need more pockets. *Tag `{TagName}` has been deleted.* {Extras.OkHand}", Save: 's');
        }

        [Command("User"), Remarks("Shows all tags owned by you or a given user."), Summary("Tag User [@User]"), Priority(1)]
        public Task UserAsync(IGuildUser User = null)
        {
            User = User ?? Context.User as IGuildUser;
            var UserTag = Context.Server.Tags.Where(x => x.Owner == User.Id).Select(x => x.Name);
            if (!Context.Server.Tags.Any() || !UserTag.Any())
                return ReplyAsync($"{Extras.Cross} Can't quite get my head around this one. *`{User}` doesn't have any tags.*");
            return PagedReplyAsync(Context.GuildHelper.Pages(UserTag), $"{User.Username} Tag Collection");
        }

        [Command("Claim"), Remarks("Claims a tag whose owner isn't in server anymore."), Summary("Tag Claim <TagName>"), Priority(1)]
        public Task ClaimAsync([Remainder] string TagName)
        {
            if (!(Context.Server.Tags.Any(t => t.Name == TagName.ToLower())))
                return ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *There is no tag with this name.*");
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == TagName);
            if ((Context.Guild as SocketGuild).Users.Where(x => x.Id == Tag.Owner).Any())
                return ReplyAsync($"{Extras.Cross} I cannot carry this. *Tag owner is still in this guild.*");
            Tag.Owner = Context.User.Id;
            return ReplyAsync($"Show me the way, Kaom. *You are now the owner of `{Tag.Name}` tag.* {Extras.OkHand}", Save: 's');
        }

        [Command("Info"), Remarks("Displays information about a given tag."), Summary("Tag Info <TagName>"), Priority(1)]
        public Task InfoAsync([Remainder] string TagName)
        {
            if (!(Context.Server.Tags.Any(t => t.Name == TagName.ToLower())))
                return SuggestTagsAsync(TagName.ToLower());
            var Tag = Context.Server.Tags.FirstOrDefault(x => x.Name == TagName);
            var User = StringHelper.ValidateUser(Context.Guild, Tag.Owner);
            return ReplyAsync(Embed: Extras.Embed(Drawing.Aqua)
                .WithAuthor($"Tag Information", Context.Client.CurrentUser.GetAvatarUrl())
                .AddField("Name", Tag.Name, true)
                .AddField("Owner", User, true)
                .AddField("Uses", Tag.Uses, true)
                .AddField("Created On", Tag.CreationDate, true)
                .AddField("Content", Tag.Content, false)
                .WithThumbnailUrl("https://png.icons8.com/nolan/80/000000/tags.png")
                .Build());
        }

        Task SuggestTagsAsync(string TagName)
        {
            var LDTags = Context.Server.Tags.Where(x => LevenshteinDistance(TagName.ToLower(), x.Name) < 5);
            var ContainTags = Context.Server.Tags.Where(x => x.Name.Contains(TagName.ToLower())).Take(5);
            var Suggested = LDTags.Union(ContainTags).Select(x => x.Name);
            return ReplyAsync($"{Extras.Cross} Must I do everything myself? *Couldn't find tag `{TagName}`* {(Suggested.Any() ? $"\nTry these:\n{string.Join("\n", Suggested)}" : null)} ");
        }

        // Honestly have no idea what this is, found it on SO from a "close string match" search
        int LevenshteinDistance(string a, string b)
        {
            if (String.IsNullOrEmpty(a) || String.IsNullOrEmpty(b))
                return 0;

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min(Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1), distances[i - 1, j - 1] + cost);
                }
            return distances[lengthA, lengthB];
        }
    }
}
