namespace PoE.Bot.Checks
{
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using Qmmands;
	using System;
	using System.Threading.Tasks;

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class RequireOwnerAttribute : CheckBaseAttribute
	{
		public override async Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
		{
			var context = ctx as GuildContext;
			var owner = (await context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner;

			if (owner.Id != context.User.Id)
				return new CheckResult(EmoteHelper.Cross + "Command can only be run by the owner of the bot.");
			else
				return CheckResult.Successful;
		}
	}
}