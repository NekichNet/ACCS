using accs.Models.Database;
using System.Linq.Expressions;

namespace accs.Repositories.Interfaces
{
    public interface IUnitRepository
    {
        public Task CreateAsync(Unit unit);
        public Task<Unit?> ReadAsync(int id);
        public Task<Unit?> FindAsync(Expression<Func<Unit, bool>> predicate);
        public Task<IEnumerable<Unit>> ReadListAsync(Expression<Func<Unit, bool>>? predicate = null);
        public Task UpdateAsync(Unit unit);
    }
}
