namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using System.Linq;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Helpers;

    public class RequireRole : PreconditionAttribute
    {
        private readonly string _requiredRole;

        public RequireRole(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Provider)
        {
            if (Context.User.Id == MethodHelper.RunSync(Context.Client.GetApplicationInfoAsync()).Owner.Id || Context.User.Id == Context.Guild.OwnerId ||
                (Context.User as SocketGuildUser).GuildPermissions.Administrator || (Context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if ((Context.User as SocketGuildUser).Roles.Any(r => r.Name == _requiredRole))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Requires `{_requiredRole}` Role*"));
        }
    }

    public class RequireModerator : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            if (Context.User.Id == MethodHelper.RunSync(Context.Client.GetApplicationInfoAsync()).Owner.Id || Context.User.Id == Context.Guild.OwnerId ||
                (Context.User as SocketGuildUser).GuildPermissions.Administrator || (Context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if ((Context.User as SocketGuildUser).Roles.Any(r => r.Name is "Moderator"))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Requires `Moderator`*"));
        }
    }

    public class RequireAdmin : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            if (Context.User.Id == MethodHelper.RunSync(Context.Client.GetApplicationInfoAsync()).Owner.Id || Context.User.Id == Context.Guild.OwnerId ||
                (Context.User as SocketGuildUser).GuildPermissions.Administrator || (Context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Requires `Admin`*"));
        }
    }
}
