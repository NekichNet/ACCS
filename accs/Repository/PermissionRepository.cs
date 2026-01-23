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

        public async Task<Permission?> ReadAsync(PermissionType permissionType)
        {
            return await _context.Permissions.FindAsync(permissionType);
        }

        public async Task<List<Permission>> ReadAllAsync()
        {
            return _context.Permissions.ToList();
        }
    }
}
