namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class RequirePermission : PreconditionAttribute
    {
        string ErrorMessage { get; set; }
        PermissionType PermissionType { get; }
        GuildPermission GuildPermission { get; }
        ChannelPermission ChannelPermission { get; }
        public RequirePermission(ChannelPermission channelPermission, string Message)
        {
            ErrorMessage = Message;
            ChannelPermission = channelPermission;
            PermissionType = PermissionType.CHANNEL;
        }

        public RequirePermission(GuildPermission guildPermission, string Message)
        {
            ErrorMessage = Message;
            GuildPermission = guildPermission;
            PermissionType = PermissionType.GUILD;
        }

        public RequirePermission() => PermissionType = PermissionType.DEFAULT;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var Context = context as IContext;
            var User = Context.User as SocketGuildUser;
            var DefaultPerms = new[] { GuildPermission.Administrator, GuildPermission.BanMembers, GuildPermission.KickMembers,
                GuildPermission.ManageChannels, GuildPermission.ManageGuild, GuildPermission.ManageMessages};
            var Special = User.Id == MethodHelper.RunSync(Context.Client.GetApplicationInfoAsync()).Owner.Id || User.Id == Context.Guild.OwnerId;
            bool Success = false;
            switch (PermissionType)
            {
                case PermissionType.CHANNEL: Success = User.GetPermissions(Context.Channel as IGuildChannel).Has(ChannelPermission) || Special; break;
                case PermissionType.GUILD: Success = User.GuildPermissions.Has(GuildPermission) || Special; break;
                case PermissionType.DEFAULT:
                    Success = DefaultPerms.Any(x => User.GuildPermissions.Has(x)) || Special;
                    ErrorMessage = $"{Extras.Cross} I am sorry, God. We must learn not to abuse your creations. *Insufficient Permissions*";
                    break;
            }
            return Success ? Task.FromResult(PreconditionResult.FromSuccess()) : Task.FromResult(PreconditionResult.FromError($"{ErrorMessage}"));
        }
    }
    public enum PermissionType
    {
        GUILD,
        DEFAULT,
        CHANNEL
    }
}
