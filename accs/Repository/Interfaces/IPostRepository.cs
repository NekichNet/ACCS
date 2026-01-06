using accs.Models;

namespace accs.Repository.Interfaces
{
    public interface IPostRepository
    {
        Task Create(Post post);
        Task<List<Post>> ReadAll();
        Task<Post?> Read(int id);
        Task Update(Post post);
        Task Delete(Post post);
    }
}
