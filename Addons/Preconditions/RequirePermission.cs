namespace PoE.Bot.Addons.Preconditions
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public enum PermissionType
    {
        Channel,
        Default,
        Guild
    }

    public class RequirePermission : PreconditionAttribute
    {
        public RequirePermission(ChannelPermission channelPermission, string message)
        {
            ErrorMessage = message;
            ChannelPermission = channelPermission;
            PermissionType = PermissionType.Channel;
        }

        public RequirePermission(GuildPermission guildPermission, string message)
        {
            ErrorMessage = message;
            GuildPermission = guildPermission;
            PermissionType = PermissionType.Guild;
        }

        public RequirePermission() => PermissionType = PermissionType.Default;

        private ChannelPermission ChannelPermission { get; }
        private string ErrorMessage { get; set; }
        private GuildPermission GuildPermission { get; }
        private PermissionType PermissionType { get; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext commandContext, CommandInfo command, IServiceProvider services)
        {
            Context context = commandContext as Context;
            SocketGuildUser user = context.User as SocketGuildUser;
            GuildPermission[] defaultPerms = new[] { GuildPermission.Administrator, GuildPermission.BanMembers, GuildPermission.KickMembers, GuildPermission.ManageChannels, GuildPermission.ManageGuild, GuildPermission.ManageMessages };
            bool special = user.Id == MethodHelper.RunSync(context.Client.GetApplicationInfoAsync()).Owner.Id || user.Id == context.Guild.OwnerId;
            bool success = false;

            switch (PermissionType)
            {
                case PermissionType.Channel:
                    success = user.GetPermissions(context.Channel as IGuildChannel).Has(ChannelPermission) || special;
                    break;

                case PermissionType.Guild:
                    success = user.GuildPermissions.Has(GuildPermission) || special;
                    break;

                case PermissionType.Default:
                    success = defaultPerms.Any(x => user.GuildPermissions.Has(x)) || special;
                    ErrorMessage = $"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Insufficient Permissions*";
                    break;
            }
            return success
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError($"{ErrorMessage}"));
        }
    }
}