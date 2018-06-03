namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class UserPermission : PreconditionAttribute
    {
        string ErrorMessage { get; set; }
        PermissionType PermissionType { get; }
        GuildPermission GuildPermission { get; }
        ChannelPermission ChannelPermission { get; }
        public UserPermission(ChannelPermission channelPermission, string Message)
        {
            ErrorMessage = Message;
            ChannelPermission = channelPermission;
            PermissionType = PermissionType.CHANNEL;
        }

        public UserPermission(GuildPermission guildPermission, string Message)
        {
            ErrorMessage = Message;
            GuildPermission = guildPermission;
            PermissionType = PermissionType.GUILD;
        }

        public UserPermission() => PermissionType = PermissionType.DEFAULT;

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
                    ErrorMessage = $"What a bummer! Are you sure you've got the proper permissions to run `{(command != null ? command.Name : "Unknown")}` command? {Extras.Cross}";
                    break;
            }
            return Success ? Task.FromResult(PreconditionResult.FromSuccess()) : Task.FromResult(PreconditionResult.FromError($"{Extras.Cross}{ErrorMessage}"));
        }
    }
    public enum PermissionType
    {
        GUILD,
        DEFAULT,
        CHANNEL
    }
}
