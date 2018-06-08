namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class BotPermission : PreconditionAttribute
    {
        GuildPermission GuildPermission { get; }
        public BotPermission(GuildPermission guildPermission)
        {
            GuildPermission = guildPermission;
        }
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            string Permission = null;
            var IsSuccess = (Context.Guild as SocketGuild).CurrentUser.GuildPermissions.Has(GuildPermission);
            switch (GuildPermission)
            {
                case GuildPermission.BanMembers: Permission = "ban users"; break;
                case GuildPermission.KickMembers: Permission = "kick users"; break;
                case GuildPermission.ManageRoles: Permission = "manage roles"; break;
                case GuildPermission.ManageMessages: Permission = "manage messages"; break;
                case GuildPermission.ManageChannels: Permission = "manage channels"; break;
            }
            return IsSuccess ? Task.FromResult(PreconditionResult.FromSuccess()) :
                Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} An emperor is only as efficient as those he commands. Missing `{Permission}` permission."));
        }
    }
}
