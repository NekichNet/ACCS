using accs.Database;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Util;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
    public class PostService : BusinessService
    {
        private readonly AppDbContext _db;

        public PostService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Post>> CreateAsync(
            string name,
            string description,
            int? subdivisionId,
            int headId,
            int maxRankId,
            string color,
            bool appendSubdivisionName
            )
        {
            ActionResult<Post> action = new ActionResult<Post>(_logger);

            try
            {
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.ManageStructure))
					{
						action.Value = new Post
						{
							Name = name,
							Description = description,
							SubdivisionId = subdivisionId,
							HeadId = headId,
							MaxRankId = maxRankId,
							Color = color,
							AppendSubdivisionName = appendSubdivisionName
						};

						action.Value.UpdateRole();

						await _db.Posts.AddAsync(action.Value);
						await _db.SaveChangesAsync();

						action.FormSuccess("Post created");
					}
					else
					{
						action.FormFailure("Post creation restricted");
					}
				}
				else
				{
					action.FormFailure("Post creation restricted. Unauthorized");
				}
			}
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
		}

        public async Task<ActionResult<Post>> GetAsync(
            int id
            )
        {
			ActionResult<Post> action = new ActionResult<Post>(_logger);

            try
            {
				action.Value = await _db.Posts.FindAsync(id);
				if (action.Value != null)
					action.FormSuccess("Post found");
				else
					action.FormFailure("Post not found");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
            }

            return action;
		}

        public async Task<ActionResult<List<Post>>> GetAllAsync()
        {
            ActionResult<List<Post>> action = new ActionResult<List<Post>>(_logger);

			try
			{
				action.Value = await _db.Posts.ToListAsync();
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

            return action.FormSuccess("Post list formed, length: " + action.Value.Count());
        }
    }
}
