using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private AppDbContext _context;

        public TicketRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Ticket ticket)
        {
			_context.Tickets.Add(ticket);
			_context.SaveChanges();
		}

		public async Task<List<Ticket>> ReadAllAsync()
        {
            return _context.Tickets.ToList();
        }

		public async Task<Ticket?> ReadAsync(int id)
        {
            return _context.Tickets.Find(id);
        }

		public async Task UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            _context.SaveChanges();
        }
    }
}
