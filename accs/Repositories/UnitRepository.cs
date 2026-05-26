using accs.Database;
using accs.Logging;
using accs.Models.Database;
using accs.Repositories.Interfaces;
using accs.Repositories.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace accs.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private AppDbContext _db;
        private ILogger<UnitRepository> _log;

        public UnitRepository(AppDbContext db, ILogger<UnitRepository> log)
        {
            _db = db;
            _log = log;
        }

		public async Task CreateAsync(Unit unit)
        {
            try
            {
				await _db.Units.AddAsync(unit);
				await _db.SaveChangesAsync();
				CRUDLogging<Unit>.LogCreated(_log, unit);
			}
            catch (Exception ex)
            {
                _log.LogError(
					eventId: EventIds.UnhandledError,
					exception: ex,
					$"Failed creating unit {unit.Nickname} with Id {unit.DiscordId}");
            }
        }

		public async Task<Unit?> ReadAsync(int id)
		{
			try
			{
				Unit? unit = await _db.Units.FindAsync(id);
				if (unit == null)
					CRUDLogging<int>.LogNotFound(_log, id);
				else
					CRUDLogging<Unit>.LogRead(_log, unit);
				return unit;
			}
			catch (Exception ex)
			{
				_log.LogError(
					eventId: EventIds.UnhandledError,
					exception: ex,
					$"Failed reading unit with Id {id}");
				return null;
			}
		}

		public async Task<Unit?> FindAsync(Expression<Func<Unit, bool>> predicate)
		{
			try
			{
				Unit? unit = await _db.Units.FirstOrDefaultAsync(predicate);
				if (unit == null)
					CRUDLogging<Expression>.LogNoData(_log, predicate);
				else
					CRUDLogging<Unit>.LogRead(_log, unit);
				return unit;
			}
			catch (Exception ex)
			{
				_log.LogError(
					eventId: EventIds.UnhandledError,
					exception: ex,
					$"Failed finding unit: {predicate.ToString()}");
				return null;
			}
		}

		public async Task<IEnumerable<Unit>> ReadListAsync(Expression<Func<Unit, bool>>? predicate = null)
		{
			IEnumerable<Unit> list = new List<Unit>();
			try
			{
				if (predicate == null)
					list = _db.Units.AsEnumerable();
				else
					list = _db.Units.Where(predicate);
				if (list.Any())
					CRUDLogging<string>.LogRead(_log, predicate.ToString() + " " + string.Join(", ", list.Select(u => u.ToString())));
				else
					CRUDLogging<Expression>.LogNoData(_log, predicate);
			}
			catch (Exception ex)
			{
				_log.LogError(
					eventId: EventIds.UnhandledError,
					exception: ex,
					$"Failed getting units: {predicate.ToString()}");
			}
			return list;
		}

		public async Task UpdateAsync(Unit unit)
		{
			try
			{
				_db.Entry(unit).State = EntityState.Modified;
				await _db.SaveChangesAsync();
				CRUDLogging<Unit>.LogUpdated(_log, unit);
			}
			catch (Exception ex)
			{
				_log.LogError(
					eventId: EventIds.UnhandledError,
					exception: ex,
					$"Failed updating unit {unit.Nickname} with Id {unit.DiscordId}");
			}
		}
    }
}
