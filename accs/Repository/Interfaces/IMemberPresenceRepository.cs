using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IMemberPresenceRepository
    {
        Task CreateAsync(MemberPresence presence);
        Task<MemberPresence?> ReadAsync(ulong discordId);
        Task<List<MemberPresence>> ReadAllAsync();
        Task UpdateAsync(MemberPresence presence);
        Task DeleteAsync(MemberPresence presence);
    }
}
