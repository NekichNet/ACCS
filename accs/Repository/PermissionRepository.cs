using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private AppDbContext _context;

        public PermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Permission?> Read(int id)
        {
            return _context.Permissions.Find(id);
        }

        public async Task<List<Permission>> ReadAll()
        {
            return _context.Permissions.ToList();
        }
    }
}
