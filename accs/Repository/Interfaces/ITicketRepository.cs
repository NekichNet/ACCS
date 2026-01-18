using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface ITicketRepository
    {
		Task CreateAsync(Ticket ticket);
		Task<List<Ticket>> ReadAllAsync();
		Task<Ticket?> ReadAsync(int id);
		Task UpdateAsync(Ticket ticket);
	}
}
