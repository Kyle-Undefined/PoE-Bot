namespace PoE.Bot.Addons.Preconditions
{
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class RequireAdmin : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User.Id == MethodHelper.RunSync(context.Client.GetApplicationInfoAsync()).Owner.Id || context.User.Id == context.Guild.OwnerId ||
                (context.User as SocketGuildUser).GuildPermissions.Administrator || (context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Requires `Admin`*"));
        }
    }

    public partial class RequireModerator : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User.Id == MethodHelper.RunSync(context.Client.GetApplicationInfoAsync()).Owner.Id || context.User.Id == context.Guild.OwnerId ||
                (context.User as SocketGuildUser).GuildPermissions.Administrator || (context.User as SocketGuildUser).GuildPermissions.ManageGuild ||
                (context.User as SocketGuildUser).GuildPermissions.ManageChannels || (context.User as SocketGuildUser).GuildPermissions.ManageRoles ||
                (context.User as SocketGuildUser).GuildPermissions.BanMembers || (context.User as SocketGuildUser).GuildPermissions.KickMembers)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Requires `Moderator`*"));
        }
    }

    public partial class RequireRole : PreconditionAttribute
    {
        private string roleName;

        public RequireRole(string requiredRole)
            => roleName = requiredRole;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider provider)
        {
            if (context.User.Id == MethodHelper.RunSync(context.Client.GetApplicationInfoAsync()).Owner.Id || context.User.Id == context.Guild.OwnerId ||
                (context.User as SocketGuildUser).GuildPermissions.Administrator || (context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if ((context.User as SocketGuildUser).Roles.Any(r => r.Name == roleName))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Requires `{roleName}` Role*"));
        }
    }
}