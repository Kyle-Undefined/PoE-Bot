namespace PoE.Bot.Modules
{
    using Addons;
    using Addons.Preconditions;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using Objects;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [Name("Tag Commands"), Group("Tag"), Ratelimit]
    public class TagModule : BotBase
    {
        [Command("Add"), Remarks("Add a tag with the specified content. To specify more than one word, wrap your name with quotes \"like this\"."), Summary("Tag Add <tagName> <tagContent>"), Priority(1)]
        public Task AddAsync(string tagName, [Remainder] string tagContent)
        {
            if (Context.Server.Tags.Any(t => t.Name == tagName.ToLower()))
                return ReplyAsync($"{Extras.Cross} You chose the road, old friend. God put me at the end of it. *There is already a tag with this name.*");

            Context.Server.Tags.Add(new TagObject
            {
                Uses = 0,
                Name = tagName.ToLower(),
                Owner = Context.User.Id,
                Content = tagContent,
                CreationDate = DateTime.Now
            });

            return ReplyAsync($"I think that will come in handy at some point. *Tag `{tagName}` has been created.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Claim"), Remarks("Claims a tag whose owner isn't in server anymore."), Summary("Tag Claim <tagName>"), Priority(1)]
        public Task ClaimAsync([Remainder] string tagName)
        {
            if (!Context.Server.Tags.Any(t => t.Name == tagName.ToLower()))
                return ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *There is no tag with this name.*");

            TagObject tag = Context.Server.Tags.FirstOrDefault(x => x.Name == tagName.ToLower());
            if ((Context.Guild as SocketGuild).Users.Any(x => x.Id == tag.Owner))
                return ReplyAsync($"{Extras.Cross} I cannot carry this. *Tag owner is still in this guild.*");

            tag.Owner = Context.User.Id;
            return ReplyAsync($"Show me the way, Kaom. *You are now the owner of `{tag.Name}` tag.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Delete"), Remarks("Deletes a tag."), Summary("Tag Delete <tagName>"), Priority(1)]
        public Task DeleteAsync([Remainder] string tagName)
        {
            if (!Context.Server.Tags.Any(t => t.Name == tagName.ToLower()))
                return ReplyAsync($"{Extras.Cross} It was possible to be happy here once. *There is no tag with this name.*");

            TagObject tag = Context.Server.Tags.FirstOrDefault(x => x.Name == tagName.ToLower());
            if (Context.User.Id != tag.Owner)
                return ReplyAsync($"{Extras.Cross} This item whispers of destiny. *You aren't the owner of `{tagName}`*");

            Context.Server.Tags.Remove(tag);
            return ReplyAsync($"I need more pockets. *Tag `{tagName}` has been deleted.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Info"), Remarks("Displays information about a given tag."), Summary("Tag Info <tagName>"), Priority(1)]
        public Task InfoAsync([Remainder] string tagName)
        {
            if (!Context.Server.Tags.Any(t => t.Name == tagName.ToLower()))
                return SuggestTagsAsync(tagName.ToLower());

            TagObject tag = Context.Server.Tags.FirstOrDefault(x => x.Name == tagName.ToLower());
            string user = Context.Guild.ValidateUser(tag.Owner);
            return ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithAuthor("Tag Information", Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl())
                .AddField("Name", tag.Name, true)
                .AddField("Owner", user, true)
                .AddField("Uses", tag.Uses, true)
                .AddField("Created On", tag.CreationDate, true)
                .AddField("Content", tag.Content, false)
                .WithThumbnailUrl("https://png.icons8.com/nolan/80/000000/tags.png")
                .Build());
        }

        [Command("List"), Remarks("Shows all tags in a list."), Summary("Tag List"), Priority(1)]
        public Task ListAsync()
            => PagedReplyAsync(MethodHelper.Pages(Context.Server.Tags.Select(t => t.Name)), "Tag Collection");

        [Command("Search"), Remarks("Shows all tags that match the search."), Summary("Tag Search <tagName>"), Priority(1)]
        public Task SearchAsync([Remainder] string tagName)
        {
            var levenTags = Context.Server.Tags.Where(x => LevenshteinDistance(tagName.ToLower(), x.Name) < 5);
            var containTags = Context.Server.Tags.Where(x => x.Name.Contains(tagName.ToLower())).Take(5);
            var searched = levenTags.Union(containTags).Select(t => t.Name);
            return PagedReplyAsync(MethodHelper.Pages(searched), "Tag Search Results");
        }

        [Command, Priority(0), Summary("Gets a tag with the given name.")]
        public Task TagAsync([Remainder] string tagName)
        {
            if (!Context.Server.Tags.Any(t => t.Name == tagName.ToLower()))
                return SuggestTagsAsync(tagName.ToLower());

            TagObject tag = Context.Server.Tags.FirstOrDefault(x => x.Name == tagName.ToLower());
            tag.Uses++;
            return ReplyAsync(tag.Content, save: DocumentType.Server);
        }

        [Command("Update"), Alias("Modify", "Change"), Remarks("Updates an existing tag. To specify more than one word, wrap your name with quotes \"like this\"."), Summary("Tag Update <tagName> <tagContent>"), Priority(1)]
        public Task UpdateAsync(string tagName, [Remainder] string tagContent)
        {
            if (!Context.Server.Tags.Any(t => t.Name == tagName.ToLower()))
                return ReplyAsync($"{Extras.Cross} Return to the dirt! *There is no tag with this name.*");

            TagObject tag = Context.Server.Tags.FirstOrDefault(x => x.Name == tagName.ToLower());
            if (Context.User.Id != tag.Owner)
                return ReplyAsync($"{Extras.Cross} I have a hunch I'll be needing this. *You aren't the owner of `{tagName}`*");

            tag.Content = tagContent;
            return ReplyAsync($"Thank you, my ancestors. I will repay your gift. *Tag `{tagName}`'s contant has been updated.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("User"), Remarks("Shows all tags owned by you or a given user."), Summary("Tag User [@user]"), Priority(1)]
        public Task UserAsync(IGuildUser user = null)
        {
            user = user ?? Context.User as IGuildUser;
            var userTag = Context.Server.Tags.Where(x => x.Owner == user.Id).Select(x => x.Name);
            return !Context.Server.Tags.Any() || !userTag.Any()
                ? ReplyAsync($"{Extras.Cross} Can't quite get my head around this one. *`{user}` doesn't have any tags.*")
                : PagedReplyAsync(MethodHelper.Pages(userTag), $"{user.Username}'s Tag Collection");
        }

        // Honestly have no idea what this is, found it on SO from a "close string match" search
        private int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0;

            int lengthA = a.Length;
            int lengthB = b.Length;
            int[,] distances = new int[lengthA + 1, lengthB + 1];
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

        private Task SuggestTagsAsync(string tagName)
        {
            var levenTags = Context.Server.Tags.Where(x => LevenshteinDistance(tagName.ToLower(), x.Name) < 5);
            var containTags = Context.Server.Tags.Where(x => x.Name.Contains(tagName.ToLower())).Take(5);
            var suggested = levenTags.Union(containTags).Select(x => x.Name);
            return ReplyAsync($"{Extras.Cross} Must I do everything myself? *Couldn't find tag `{tagName}`* {(suggested.Any() ? $"\nTry these:\n{string.Join("\n", suggested)}" : null)} ");
        }
    }
}