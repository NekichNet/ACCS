
using accs.Database;
using accs.Models;
using accs.Models.Util;
using accs.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
    public class StructureService : BusinessService
    {
        private readonly AppDbContext _db;

        public StructureService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Dictionary<string, object>>> GetStructureAsync()
        {
            ActionResult<Dictionary<string, object>> action = new ActionResult<Dictionary<string, object>>(_logger);

            try
            {
                var posts = await _db.Posts.ToListAsync();
                var units = await _db.Units
                    .Include(u => u.AssignedPosts)
                        .ThenInclude(ap => ap.Post)
                    .ToListAsync();

                var rootPost = posts.FirstOrDefault(p => p.HeadId == null);

                if (rootPost == null)
                {
                    action.FormFailure("Root post not found");
                    return action;
                }

                action.Value = new Dictionary<string, object>
                {
                    { rootPost.Name, BuildPostStructure(rootPost, posts, units) }
                };

                action.FormSuccess("Structure formed");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        private Dictionary<string, object> BuildPostStructure(Post post, List<Post> allPosts, List<Unit> allUnits)
        {
            var people = allUnits
                .Where(u => u.GetPosts().Any(p => p.Id == post.Id))
                .Select(u => u.Nickname)
                .ToList();

            var subordinates = allPosts
                .Where(p => p.HeadId == post.Id)
                .ToDictionary(
                    p => p.Name,
                    p => BuildPostStructure(p, allPosts, allUnits)
                );

            return new Dictionary<string, object>
            {
                { "Id", post.Id },
                { "Color", post.Color ?? "#FFFFFF" },
                { "People", people },
                { "Subdivision", post.SubdivisionId },
                { "Subordinates", subordinates }
            };
        }
    }
}
