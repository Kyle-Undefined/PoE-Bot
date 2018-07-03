namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class IntCriteria : ICriteria<SocketMessage>
    {
        public Task<bool> JudgeAsync(Context context, SocketMessage param)
            => Task.FromResult(int.TryParse(param.Content, out _));
    }
}