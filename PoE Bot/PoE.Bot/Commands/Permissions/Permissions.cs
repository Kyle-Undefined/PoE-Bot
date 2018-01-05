namespace PoE.Bot.Commands.Permissions
{
    public enum Permission : ulong
    {
        None = 0x0000000000000000u,
        CreateInstantInvite = 0x0000000000000001u,
        KickMembers = 0x0000000000000002u,
        BanMembers = 0x0000000000000004u,
        Administrator = 0x0000000000000008u,
        ManageChannels = 0x0000000000000010u,
        ManageGuild = 0x0000000000000020u,
        AddReactions = 0x0000000000000040u,
        ReadMessages = 0x0000000000000400u,
        SendMessages = 0x0000000000000800u,
        SendTtsMessages = 0x0000000000001000u,
        ManageMessages = 0x0000000000002000u,
        EmbedLinks = 0x0000000000004000u,
        AttachFiles = 0x0000000000008000u,
        ReadMessageHistory = 0x0000000000010000u,
        MentionEveryone = 0x0000000000020000u,
        UseExternalEmojis = 0x0000000000040000u,
        UseVoice = 0x0000000000100000u,
        Speak = 0x0000000000200000u,
        MuteMembers = 0x0000000000400000u,
        DeafenMembers = 0x0000000000800000u,
        MoveMembers = 0x0000000001000000u,
        UserVoiceDetection = 0x0000000002000000u,
        ChangeNickname = 0x0000000004000000u,
        ManageNicknames = 0x0000000008000000u,
        ManageRoles = 0x0000000010000000u,
        ManageWebhooks = 0x0000000020000000u,
        ManageEmoji = 0x0000000040000000u
    }
}
