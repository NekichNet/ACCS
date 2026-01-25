using Discord.WebSocket;

namespace accs.Services.Interfaces
{
    public interface IGuildProviderService
    {
        SocketGuild GetGuild();
        ulong GetGuildId();
    }
}
