using Discord.WebSocket;

namespace accs.Services.Interfaces
{
    public interface IMemberCleanupService
    {
        Task CleanupAsync(SocketGuild guild);
    }
}
