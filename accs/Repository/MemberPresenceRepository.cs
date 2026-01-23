using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class MemberPresenceRepository : IMemberPresenceRepository
    {
        private AppDbContext _context;
        public MemberPresenceRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task CreateAsync(MemberPresence presence)
        {
            _context.MemberPresences.Add(presence);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(MemberPresence presence)
        {
            _context.MemberPresences.Remove(presence);
            await _context.SaveChangesAsync();
        }

        public async Task<List<MemberPresence>> ReadAllAsync()
        {
            return _context.MemberPresences.ToList();
        }

        public async Task<MemberPresence?> ReadAsync(ulong discordId)
        {
            return await _context.MemberPresences.FindAsync(discordId);
        }

        public async Task UpdateAsync(MemberPresence presence)
        {
            _context.MemberPresences.Update(presence);
            await _context.SaveChangesAsync();
        }
    }
}
