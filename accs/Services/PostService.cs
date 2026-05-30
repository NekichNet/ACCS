using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
using accs.Models.SingleDayEvents;
using accs.Models.Statuses;
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
				action.FormSuccess("Post list formed, length: " + action.Value.Count());
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
        }

		public async Task<ActionResult<AssignedPost>> AssignPost(ulong unitDiscordId, int postId)
		{ // Это хороший пример того, как все проверки выглядят во вложенных if. Это нужно исправить
			ActionResult<AssignedPost> action = new ActionResult<AssignedPost>(_logger);

			try
			{
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.AssignPosts))
					{
						Unit? unit = await _db.Units.FindAsync(unitDiscordId);
						if (unit != null)
						{
							if (!Actor.GetPosts().SelectMany(p => p.GetAllHeadsRecursive()).Intersect(unit.GetPosts()).Any() || Actor.IsAdmin())
							{
								Post? post = await _db.Posts.FindAsync(postId);
								if (post != null)
								{
									if (!Actor.GetPosts().SelectMany(p => p.GetAllHeadsRecursive()).Contains(post) || Actor.IsAdmin())
									{
										AssignedPost assignedPost = new AssignedPost
										{
											Unit = unit,
											Post = post
										};
										action.Value = assignedPost;

										await _db.AssignedPosts.AddAsync(assignedPost);

										await _db.SaveChangesAsync();

										action.FormSuccess($"Unit {unit.Nickname} was assigned to post {post.GetFullName()}", eventId: EventIds.Created);
									}
									else
									{
										action.FormFailure($"Post assignment restricted. Can't assign heads' post", eventId: EventIds.Forbidden);
									}
								}
								else
								{
									action.FormFailure($"Post with ID {postId} not found", eventId: EventIds.NotFound);
								}
							}
							else
							{
								action.FormFailure($"Post assignment restricted. Can't change heads' posts", eventId: EventIds.Forbidden);
							}
						}
						else
						{
							action.FormFailure($"Unit with ID {unitDiscordId} not found", eventId: EventIds.NotFound);
						}
					}
					else
					{
						action.FormFailure("Post assignment restricted", eventId: EventIds.Forbidden);
					}
				}
				else
				{
					action.FormFailure("Post assignment restricted. Unauthorized", eventId: EventIds.Unauthorized);
				}
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<>
    }
}
