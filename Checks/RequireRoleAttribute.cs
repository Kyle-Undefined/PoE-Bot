namespace PoE.Bot.Checks
{
	using Discord;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class RequireAdminAttribute : CheckBaseAttribute
	{
		public override async Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
		{
			var context = ctx as GuildContext;
			var botOwner = (await context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
			var user = context.User;
			if (botOwner == user.Id || user.Id == context.Guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild)
				return CheckResult.Successful;

			return new CheckResult(EmoteHelper.Cross + " I am sorry, God. We must learn not to abuse your creations. *Requires `Admin`*");
		}
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class RequireModeratorAttribute : CheckBaseAttribute
	{
		public override async Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
		{
			var context = ctx as GuildContext;
			var botOwner = (await context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
			var user = context.User;
			if (botOwner == user.Id || user.Id == context.Guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels
				|| user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
			{
				return CheckResult.Successful;
			}

			return new CheckResult(EmoteHelper.Cross + " I am sorry, God. We must learn not to abuse your creations. *Requires `Moderator`*");
		}
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class RequireRoleAttribute : CheckBaseAttribute
	{
		private readonly string[] _roleNames;

		public RequireRoleAttribute(params string[] roleNames) => _roleNames = roleNames;

		public override async Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
		{
			var context = ctx as GuildContext;
			var botOwner = (await context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
			var user = context.User;
			if (botOwner == user.Id || context.User.Id == context.Guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild)
				return CheckResult.Successful;

			if ((context.User as IGuildUser)?.RoleIds.Intersect(context.Guild.Roles.Where(x => _roleNames.Contains(x.Name)).Select(x => x.Id)).Any() is true)
				return CheckResult.Successful;

			return new CheckResult(EmoteHelper.Cross + " I am sorry, God. We must learn not to abuse your creations. *Requires `" + string.Join(", ", _roleNames) + "` Role *");
		}
	}
}