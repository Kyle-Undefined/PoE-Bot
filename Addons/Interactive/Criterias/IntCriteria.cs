namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class IntCriteria : ICriteria<SocketMessage>
    {
        public Task<bool> JudgeAsync(IContext Context, SocketMessage Param)
            => Task.FromResult(int.TryParse(Param.Content, out _));
    }
}
