using accs.Database;
using accs.Logging;
using accs.Models;
using accs.Models.Enums;
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
            bool appendSubdivisionName,
			List<int> permissionsId
            )
        {
            ActionResult<Post> action = new ActionResult<Post>(_logger);

            try
            {
				if (Actor != null)
				{
					if (Actor.HasPermission(PermissionType.ManageStructure))
					{
						Post? headPost = await _db.Posts.FindAsync(headId);
						if (headPost != null)
						{
							IEnumerable<Post> actorOwnPosts = Actor.GetPosts().SelectMany(p => p.GetAllSubordinatesRecursive()).Concat(Actor.GetPosts());
							if (actorOwnPosts.Contains(headPost))
							{
								Subdivision? subdivision = await _db.Subdivisions.FindAsync(subdivisionId);
								if (subdivision != null)
								{
									if (!subdivision.Posts.Any(p => !actorOwnPosts.Contains(p)))
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
										action.FormFailure($"Post creation failed. Subdivision contains posts not in subordinates", eventId: EventIds.Forbidden);
									}
								}
								else
								{
									action.FormFailure($"Post creation failed. Subdivision not found", eventId: EventIds.NotFound);
								}
							}
							else
							{
								action.FormFailure($"Post creation failed. Head post is should be one of subordinates", eventId: EventIds.Forbidden);
							}
						}
						else
						{
							action.FormFailure($"Post creation failed. Head post with ID {headId} not found", eventId: EventIds.NotFound);
						}
					}
					else
					{
						action.FormFailure("Post creation restricted", eventId: EventIds.Forbidden);
					}
				}
				else
				{
					action.FormFailure("Post creation restricted. Unauthorized", eventId: EventIds.Unauthorized);
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

		public async Task<ActionResult<AssignedPost>> AssignAsync(ulong unitDiscordId, int postId)
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

		public async Task<EmptyAction> DeposeAsync(ulong unitDiscordId, int postId)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Unit deposing restricted. Unauthorized", eventId: EventIds.Forbidden);
				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure("Unit deposing restricted.", eventId: EventIds.Forbidden);

				Unit? unit = await _db.Units.FindAsync(unitDiscordId);
				if (unit == null)
					return action.FormFailure($"Unit deposing failed. Unit with Discord ID {unitDiscordId} not found", eventId: EventIds.NotFound);
				if (unit.GetPosts().Count == 0)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit have no assigned posts", eventId: EventIds.ImpossibleAction);
				if (unit.GetPosts().Count == 1)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit has last post assigned", eventId: EventIds.ImpossibleAction);
				
				AssignedPost? assignedPost = await _db.AssignedPosts.FirstOrDefaultAsync(ap => ap.PostId == postId && ap.IsActive());
				if (assignedPost == null)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit isn't assigned to post with ID {postId}", eventId: EventIds.InvalidData);
				if (!Actor.IsAdmin() && Actor.GetPosts().SelectMany(p => p.GetAllHeadsRecursive()).Contains(assignedPost.Post))
					return action.FormFailure($"Unit {unit.Nickname} deposing resctricted. Post {assignedPost.Post.Name} is one of heads' posts");
				
				assignedPost.Terminate();

				await _db.SaveChangesAsync();

				action.FormSuccess($"Unit {unit.Nickname} deposed from {assignedPost.Post.Name}", eventId: EventIds.Ok);
			}
			catch (Exception ex)
			{
				return action.FormException(ex);
			}

			return action;
		}
    }
}
