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

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo Command, IServiceProvider Provider)
        {
            var Context = context as IContext;
            var User = Context.User as SocketGuildUser;
            var Special = User.Id == MethodHelper.RunSync(Context.Client.GetApplicationInfoAsync()).Owner.Id || User.Id == Context.Guild.OwnerId || User.GuildPermissions.Administrator || User.GuildPermissions.ManageGuild;
            var Role = User.Roles.Where(r => r.Name == _requiredRole);
            bool Success = Role.Any() || Special;
            return Success ? Task.FromResult(PreconditionResult.FromSuccess()) : Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} {(Command != null ? Command.Name : "Unknown")} requires the **{_requiredRole}** Role."));
        }
    }
}
