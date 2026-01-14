using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IPostRepository
    {
        Task CreateAsync(Post post);
        Task<List<Post>> ReadAllAsync();
        Task<Post?> ReadAsync(int id);
        Task UpdateAsync(Post post);
        Task DeleteAsync(Post post);
    }
}
