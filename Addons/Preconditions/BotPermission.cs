namespace PoE.Bot.Addons.Preconditions
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System;
    using System.Threading.Tasks;

    public class BotPermission : PreconditionAttribute
    {
        public BotPermission(GuildPermission guildPermission)
            => GuildPermission = guildPermission;

        private GuildPermission GuildPermission { get; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            string permission = null;
            switch (GuildPermission)
            {
                case GuildPermission.BanMembers:
                    permission = "ban users";
                    break;

                case GuildPermission.KickMembers:
                    permission = "kick users";
                    break;

                case GuildPermission.ManageRoles:
                    permission = "manage roles";
                    break;

                case GuildPermission.ManageMessages:
                    permission = "manage messages";
                    break;

                case GuildPermission.ManageChannels:
                    permission = "manage channels";
                    break;
            }
            return (context.Guild as SocketGuild).CurrentUser.GuildPermissions.Has(GuildPermission)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} An emperor is only as efficient as those he commands. Missing `{permission}` permission."));
        }
    }
}