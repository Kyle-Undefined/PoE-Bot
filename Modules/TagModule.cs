namespace PoE.Bot.Modules
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Addons.Interactive;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using PoE.Bot.ModuleBases;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Tag Module")]
	[Description("Tag Commands")]
	[Group("Tag")]
	public class TagModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }

		[Command("Add")]
		[Name("Tag Add")]
		[Description("Add a tag with the specified content")]
		[Usage("tag add hello-world this is a new tag")]
		[Priority(1)]
		public async Task TagAddAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			string tagName,
			[Name("Tag Content")]
			[Description("Content that will be displayed when the tag is used")]
			[Remainder] string tagContent)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Tags).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (TryParseTag(tagName, guild, out var tag))
			{
				await ReplyAsync(EmoteHelper.Cross + " You chose the road, old friend. God put me at the end of it. *There is already a tag with this name.*");
				return;
			}

			await Database.Tags.AddAsync(new Tag
			{
				Content = tagContent,
				CreationDate = DateTime.Now,
				Name = tagName,
				UserId = Context.User.Id,
				Uses = 0,
				GuildId = guild.Id
			});

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Claim")]
		[Name("Tag Claim")]
		[Description("Claims a tag whose owner isn't in guild anymore")]
		[Usage("tag claim hello-world")]
		[Priority(1)]
		public async Task TagClaimAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			[Remainder] string tagName)
		{
			var guild = await Database.Guilds.Include(x => x.Tags).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseTag(tagName, guild, out var tag))
			{
				await ReplyAsync(EmoteHelper.Cross + " I don't think I need to be doing that right now. *There is no tag with this name.*");
				return;
			}

			if (Context.Guild.Users.Any(x => x.Id == tag.UserId) && (Context.User.Id != Context.Guild.OwnerId || !Context.User.GuildPermissions.Administrator
				|| !Context.User.GuildPermissions.ManageGuild || !Context.User.GuildPermissions.ManageChannels || !Context.User.GuildPermissions.ManageRoles))
			{
				await ReplyAsync(EmoteHelper.Cross + " I cannot carry this. *Tag owner is still in this guild.*");
				return;
			}

			tag.UserId = Context.User.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Delete")]
		[Name("Tag Delete")]
		[Description("Deletes a tag that you own")]
		[Usage("tag delete hello-world")]
		[Priority(1)]
		public async Task TagDeleteAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			[Remainder] string tagName)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Tags).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseTag(tagName, guild, out var tag))
			{
				await ReplyAsync(EmoteHelper.Cross + " It was possible to be happy here once. *There is no tag with this name.*");
				return;
			}

			if (Context.User.Id != tag.UserId && (Context.User.Id != Context.Guild.OwnerId || !Context.User.GuildPermissions.Administrator
				|| !Context.User.GuildPermissions.ManageGuild || !Context.User.GuildPermissions.ManageChannels || !Context.User.GuildPermissions.ManageRoles))
			{
				await ReplyAsync(EmoteHelper.Cross + " This item whispers of destiny. *You aren't the owner of `" + tag.Name + "`*");
				return;
			}

			Database.Tags.Remove(tag);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Info")]
		[Name("Tag Info")]
		[Description("Displays information about a given tag.")]
		[Usage("tag info hello-world")]
		[Priority(1)]
		public async Task InfoAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			[Remainder] string tagName)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Tags).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseTag(tagName, guild, out var tag))
			{
				await SuggestTags(tagName, guild);
				return;
			}

			await ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor("Tag Information", Context.Guild.GetUser(tag.UserId).GetAvatar())
				.WithThumbnailUrl(Context.Guild.GetUser(tag.UserId).GetAvatar())
				.AddField("Name", tag.Name, true)
				.AddField("Owner", Context.Guild.GetUser(tag.UserId).GetDisplayName(), true)
				.AddField("Uses", tag.Uses, true)
				.AddField("Created On", tag.CreationDate, true)
				.AddField("Content", tag.Content, true)
				.Build());
		}

		[Command("List")]
		[Name("Tag List")]
		[Description("Shows all tags in the guild")]
		[Usage("Tag List")]
		[Priority(1)]
		public async Task ListAsync()
		{
			var tags = await Database.Tags.AsNoTracking().Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			var pages = new List<string>();

			foreach (var item in tags.SplitList())
				pages.Add(string.Join("\n", item.Select(x => "*" + x.Name + "*\n" + x.Content + "\n")));

			await PagedReplyAsync(new PaginatedMessage
			{
				Pages = pages,
				Color = new Color(0, 255, 255),
				Title = "List of Tags and their Content",
				Author = new EmbedAuthorBuilder
				{
					Name = "Tag Collection",
					IconUrl = Context.Client.CurrentUser.GetAvatar()
				}
			});
		}

		[Command("Search")]
		[Name("Tag Search")]
		[Description("Shows all tags that match the search")]
		[Usage("tag search hello-world")]
		[Priority(1)]
		public async Task SearchAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			[Remainder] string tagName)
		{
			var tags = await Database.Tags.AsNoTracking().Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			var levenTags = tags.Where(x => tagName.LevenshteinDistance(x.Name) < 5);
			var containTags = tags.Where(x => x.Name.Contains(tagName)).Take(5);
			var distinct = new HashSet<Tag>(levenTags.Concat(containTags));

			if (distinct.Count > 0)
			{
				var searched = distinct.Select(x => "*" + x.Name + "*\n" + x.Content + "\n").ToList().SplitList();
				var pages = new List<string>();

				foreach (var item in searched)
					pages.Add(string.Join("\n", item));

				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = "List of Tags and their Content",
					Author = new EmbedAuthorBuilder
					{
						Name = "Tag Search Results",
						IconUrl = Context.Client.CurrentUser.GetAvatar()
					}
				});
			}
			else
			{
				await ReplyAsync(EmoteHelper.Cross + " No tags found with name like `" + tagName + "`");
			}
		}

		[Command]
		[Name("Tag")]
		[Description("Gets a tag with the given name")]
		[Usage("tag hello-world")]
		[Priority(0)]
		public async Task TagAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			[Remainder] string tagName)
		{
			var guild = await Database.Guilds.Include(x => x.Tags).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseTag(tagName, guild, out var tag))
			{
				await SuggestTags(tagName, guild);
				return;
			}

			tag.Uses++;
			await Database.SaveChangesAsync();
			await ReplyAsync(tag.Content);
		}

		[Command("Update")]
		[Name("Tag Update")]
		[Description("Updates an existing tag")]
		[Usage("tag update hello-world new content")]
		[Priority(1)]
		public async Task TagUpdateAsync(
			[Name("Tag Name")]
			[Description("Name of the tag. To specify more than one word, wrap your name with quotes \"like this\"")]
			string tagName,
			[Name("Tag Content")]
			[Description("Content that will be displayed when the tag is used")]
			[Remainder] string tagContent)
		{
			var guild = await Database.Guilds.Include(x => x.Tags).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseTag(tagName, guild, out var tag))
			{
				await ReplyAsync(EmoteHelper.Cross + " Return to the dirt! *There is no tag with this name.*");
				return;
			}

			if (Context.User.Id != tag.UserId && (Context.User.Id != Context.Guild.OwnerId || !Context.User.GuildPermissions.Administrator
				|| !Context.User.GuildPermissions.ManageGuild || !Context.User.GuildPermissions.ManageChannels || !Context.User.GuildPermissions.ManageRoles))
			{
				await ReplyAsync(EmoteHelper.Cross + " I have a hunch I'll be needing this. *You aren't the owner of `" + tag.Name + "`*");
				return;
			}

			tag.Content = tagContent;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("User")]
		[Name("Tag User")]
		[Description("Shows all tags owned by you or a given user")]
		[Usage("tag user @user")]
		[Priority(1)]
		public async Task UserAsync(
			[Name("User")]
			[Description("The user whose tags you want to view, if no user is set it shows yours, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user = null)
		{
			user = user ?? Context.User;
			var tags = await Database.Tags.AsNoTracking().Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id && x.UserId == user.Id).Select(x => "*" + x.Name + "*\n" + x.Content + "\n").ToListAsync();

			if (tags.Count is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " Can't quite get my head around this one. *`" + user + "` doesn't have any tags.*");
				return;
			}
			else
			{
				var pages = new List<string>();

				foreach (var item in tags.SplitList())
					pages.Add(string.Join("\n", item));

				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = user.GetDisplayName() + "'s Tag Collection",
					Author = new EmbedAuthorBuilder
					{
						Name = "User Tags",
						IconUrl = user.GetAvatar()
					}
				});
			}
		}

		private bool TryParseTag(string tagName, Guild guild, out Tag tag)
		{
			tag = guild.Tags.FirstOrDefault(x => string.Equals(x.Name, tagName, StringComparison.CurrentCultureIgnoreCase));
			return !(tag is null);
		}

		private Task SuggestTags(string tagName, Guild guild)
		{
			var levenTags = guild.Tags.Where(x => tagName.LevenshteinDistance(x.Name) < 5);
			var containTags = guild.Tags.Where(x => x.Name.Contains(tagName)).Take(5);
			var distinct = new HashSet<Tag>(levenTags.Concat(containTags));

			if(distinct.Count > 0)
			{
				var suggested = distinct.Select(x => x.Name);
				return ReplyAsync(EmoteHelper.Cross + " Must I do everything myself? *Couldn't find tag `" + tagName + "`*\nTry these:\n" + string.Join("\n", suggested));
			}

			return ReplyAsync(EmoteHelper.Cross + " Must I do everything myself? *Couldn't find tag `" + tagName + "` and no matching tags*");
		}
	}
}