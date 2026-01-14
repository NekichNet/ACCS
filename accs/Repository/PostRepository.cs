using accs.Models;
using accs.Repository.Context;
using accs.Repository.Interfaces;

namespace accs.Repository
{
    public class PostRepository : IPostRepository
    {
        private AppDbContext _context;

        public PostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Post post)
        {
            _context.Posts.Add(post);
            _context.SaveChanges();
        }

        public async Task DeleteAsync(Post post)
        {
            _context.Posts.Remove(post);
            _context.SaveChanges();
        }

        public async Task<Post?> ReadAsync(int id)
        {
            return _context.Posts.Find(id);
        }

        public async Task<List<Post>> ReadAllAsync()
        {
            return _context.Posts.ToList();
        }

        public async Task UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            _context.SaveChanges();
        }
    }
}
