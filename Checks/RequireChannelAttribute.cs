namespace PoE.Bot.Checks
{
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using Qmmands;
	using System;
	using System.Threading.Tasks;

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class BanChannelAttribute : CheckBaseAttribute
	{
		private readonly string _channel;

		public BanChannelAttribute(string channel) => _channel = channel;

		public override Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
		{
			var context = ctx as GuildContext;
			if (context.Channel.Name == _channel)
				return Task.FromResult(new CheckResult(EmoteHelper.Cross + " The ancestors watch over me, can not use command in the `" + _channel + "` channel."));

			return Task.FromResult(CheckResult.Successful);
		}
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class RequireChannelAttribute : CheckBaseAttribute
	{
		private readonly string _channel;

		public RequireChannelAttribute(string channel) => _channel = channel;

		public override Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
		{
			var context = ctx as GuildContext;
			if (context.Channel.Name == _channel || context.Channel.Name.StartsWith(_channel))
				return Task.FromResult(CheckResult.Successful);

			return Task.FromResult(new CheckResult(EmoteHelper.Cross + " The ancestors watch over me, use command in the `" + _channel + "` channel."));
		}
	}
}