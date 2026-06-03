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
		private readonly RankService _rankService;
		private readonly SubdivisionService _subdivisionService;

        public PostService(
			AppDbContext db,
			RankService rankService,
			SubdivisionService subdivisionService,
			ILogger logger)
			: base(logger)
        {
            _db = db;
			_rankService = rankService;
			_subdivisionService = subdivisionService;
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
				ActionResult<Post> headResult = await CheckCanManageAsync(headId);
				if (!headResult.IsSuccess)
					return action.FormFailure("Permission check failed", eventId: EventIds.Forbidden);

				if (subdivisionId != null)
				{
					_subdivisionService.Actor = Actor;
					ActionResult<Subdivision> subdivResult = await _subdivisionService.CheckCanManageAsync((int)subdivisionId);
					if (!subdivResult.IsSuccess)
						return action.FormFailure($"Can't assign post to subdivision with ID {subdivisionId}", eventId: EventIds.Forbidden);
				}

				Rank? rank = Actor.GetMaxRank();
				if (rank == null)
					return action.FormFailure("Post creation failed. Can't get user's max available rank", eventId: EventIds.NotFound);
				
				ActionResult<Rank> rankResult = await _rankService.GetAsync(maxRankId);
				if (!rankResult.IsSuccess)
					return action.FormFailure($"Post creation failed. Rank with ID {maxRankId} not found", eventId: EventIds.NotFound);

				if (rank.GetAllHigherRecursive().Contains(rankResult.Value))
					return action.FormFailure($"Post creation failed. Max rank is higher, than user's max rank", eventId: EventIds.Forbidden);

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

				action.FormSuccess($"Post {action.Value.GetFullName()} created");
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

		public async Task<EmptyAction> UpdateAsync(
			int postId,
			string name,
			bool appendSubdivisionName,
			string description,
			string color,
			int? subdivisionId,
			int maxRankId,
			int headId
			)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				ActionResult<Post> result = await CheckCanManageAsync(postId);
				if (!result.IsSuccess)
					return action.FormFailure("Permission check failed", eventId: EventIds.Forbidden);

				ActionResult<Post> headResult = await CheckCanManageAsync(headId);
				if (!headResult.IsSuccess)
					return action.FormFailure($"Post updating failed. Can't set head with ID {postId}", eventId: EventIds.Forbidden);

				if (subdivisionId != null)
				{
					_subdivisionService.Actor = Actor;
					ActionResult<Subdivision> subdivResult = await _subdivisionService.CheckCanManageAsync((int)subdivisionId);
					if (!subdivResult.IsSuccess)
						return action.FormFailure($"Can't assign post to subdivision with ID {subdivisionId}", eventId: EventIds.Forbidden);
				}

				Rank? rank = Actor.GetMaxRank();
				if (rank == null)
					return action.FormFailure("Post updating failed. Can't get user's max available rank", eventId: EventIds.NotFound);

				ActionResult<Rank> rankResult = await _rankService.GetAsync(maxRankId);
				if (!rankResult.IsSuccess)
					return action.FormFailure($"Post updating failed. Rank with ID {maxRankId} not found", eventId: EventIds.NotFound);

				if (rank.GetAllHigherRecursive().Contains(rankResult.Value))
					return action.FormFailure($"Post updating failed. Max rank is higher, than user's max rank", eventId: EventIds.Forbidden);

				result.Value.Name = name;
				result.Value.Description = description;
				result.Value.SubdivisionId = subdivisionId;
				result.Value.HeadId = headId;
				result.Value.MaxRankId = maxRankId;
				result.Value.Color = color;
				result.Value.AppendSubdivisionName = appendSubdivisionName;

				_db.Posts.Update(result.Value);
				await _db.SaveChangesAsync();

				result.Value.UpdateRole();

				action.FormSuccess("Post updated");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		/// <summary>
		/// Обновляет Discord роль должности или создаёт её, если не существует
		/// </summary>
		/// <param name="postId">ID должности</param>
		/// <returns>ActionResult с Discord ID роли должности</returns>
		public async Task<ActionResult<ulong?>> UpdateRoleAsync(int postId)
		{
			ActionResult<ulong?> action = new ActionResult<ulong?>(_logger);

			try
			{
				ActionResult<Post> result = await CheckCanManageAsync(postId);

				if (!result.IsSuccess)
					return action.FormFailure("Permission check failed");

				result.Value.UpdateRole();
				_db.Posts.Update(result.Value);
				await _db.SaveChangesAsync();
				action.Value = result.Value.DiscordRoleId;
				action.FormSuccess("Post Discord role updated");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<EmptyAction> DeleteAsync(int postId)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				ActionResult<Post> result = await CheckCanManageAsync(postId);

				if (!result.IsSuccess)
					return action.FormFailure("Permission check failure", eventId: EventIds.Forbidden);

				_db.Posts.Remove(result.Value);

				await _db.SaveChangesAsync();

				action.FormSuccess($"Post {result.Value.Name} deleted", eventId: EventIds.Deleted);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}


		public async Task<ActionResult<List<AssignedPost>>> SetAssignedPosts(ulong unitDiscordId, int[] postIds)
		{
			ActionResult<List<AssignedPost>> action = new ActionResult<List<AssignedPost>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Posts setting restricted. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure("Posts setting restricted", eventId: EventIds.Forbidden);

				if (postIds.Length == 0)
					return action.FormFailure($"Posts setting failure. Can't set zero posts", eventId: EventIds.InvalidData);

				Unit? unit = await _db.Units.FindAsync(unitDiscordId);
				if (unit == null)
					return action.FormFailure($"Posts setting failure. Unit with ID {unitDiscordId} not found", eventId: EventIds.NotFound);
				if (!unit.IsActive())
					return action.FormFailure($"Posts setting restricted. Unit {unit.Nickname} is in retirement or dismissed", eventId: EventIds.Forbidden);

				List<AssignedPost> assignedPosts = unit.GetAssignedPosts();

				ushort assignedCounter = 0;
				foreach (int postId in postIds)
				{
					ActionResult<AssignedPost> result = await AssignAsync(unitDiscordId, postId, unit);
					if (result.IsSuccess)
						assignedCounter++;
				}

				if (assignedCounter == 0)
					return action.FormFailure("Failed to assign any of provided posts", eventId: EventIds.Failed);

				ushort deposedCounter = 0;
				foreach (AssignedPost assignedPost in assignedPosts)
				{
					EmptyAction result = await DeposeAsync(unitDiscordId, assignedPost.Post.Id, unit, assignedPost);
					if (result.IsSuccess)
						deposedCounter++;
				}

				action.FormSuccess(
					$"{unit.GetRankName()} {unit.Nickname} was assigned to {assignedCounter} posts, deposed from {deposedCounter} posts",
					eventId: EventIds.Updated
				);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<AssignedPost>> AssignAsync(ulong unitDiscordId, int postId, Unit? unit = null)
		{ // Это хороший пример того, как все проверки выглядят во вложенных if. Это нужно исправить
			ActionResult<AssignedPost> action = new ActionResult<AssignedPost>(_logger);

			try
			{
				ActionResult<Post> result = await CheckCanAssignAsync(postId);
				if (!result.IsSuccess)
					return action.FormFailure("Permission check failed", eventId: EventIds.Forbidden);

				if (unit == null)
					unit = await _db.Units.FindAsync(unitDiscordId);
				if (unit == null)
					return action.FormFailure($"Post assigning failed. Unit with ID {unitDiscordId} not found", eventId: EventIds.NotFound);

                AssignedPost assignedPost = new AssignedPost
				{
					Unit = unit,
					Post = result.Value
				};
				action.Value = assignedPost;

				await _db.AssignedPosts.AddAsync(assignedPost);

				await _db.SaveChangesAsync();

				action.FormSuccess($"Unit {unit.Nickname} was assigned to post {result.Value.GetFullName()}", eventId: EventIds.Created);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<EmptyAction> DeposeAsync(ulong unitDiscordId, int postId, Unit? unit = null, AssignedPost? assignedPost = null)
		{
			EmptyAction action = new EmptyAction(_logger);

			try
			{
				Post? post = null;
				if (assignedPost != null)
					post = assignedPost.Post;
				EmptyAction result = await CheckCanAssignAsync(postId, post);
				if (!result.IsSuccess)
					return action.FormFailure("Permission check failure", eventId: EventIds.Forbidden);

				if (unit == null)
					unit = await _db.Units.FindAsync(unitDiscordId);
				if (unit == null)
					return action.FormFailure($"Unit deposing failed. Unit with Discord ID {unitDiscordId} not found", eventId: EventIds.NotFound);
				if (!unit.IsActive())
					return action.FormFailure($"Unit {unit.Nickname} deposing impossible. Unit is in retirement or dismissed", eventId: EventIds.ImpossibleAction);
				if (unit.GetPosts().Count == 0)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit have no assigned posts", eventId: EventIds.ImpossibleAction);
				if (unit.GetPosts().Count == 1)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit has last post assigned", eventId: EventIds.ImpossibleAction);
				
				if (assignedPost == null)
					assignedPost = await _db.AssignedPosts.FirstOrDefaultAsync(ap => ap.UnitId == unitDiscordId && ap.PostId == postId && ap.IsActive());
				if (assignedPost == null)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit isn't assigned to post with ID {postId}", eventId: EventIds.NotFound);
				
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

		public async Task<ActionResult<Post>> CheckCanManageAsync(int postId, Post? post = null)
		{
			ActionResult<Post> action = new ActionResult<Post>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't check permissions. Unauthorized", eventId: EventIds.Unauthorized);

				if (post == null)
					post = await _db.Posts.FindAsync(postId);
				action.Value = post;

				if (action.Value == null)
					return action.FormFailure($"Can't check permissions. Post with ID {postId} not found", eventId: EventIds.NotFound);

				List<Post> actorControllablePosts = Actor.GetPosts().SelectMany(p => p.GetAllSubordinatesRecursive()).ToList();

				if (!Actor.HasPermission(PermissionType.ManageStructure))
					return action.FormFailure($"{Actor.Nickname} don't have ManageStructure permission", eventId: EventIds.Forbidden);
				else if (!Actor.IsAdmin() && !actorControllablePosts.Contains(action.Value))
					return action.FormFailure($"Post {action.Value.GetFullName()} isn't under {Actor.Nickname}'s control", eventId: EventIds.Forbidden);

				action.FormSuccess($"{Actor.Nickname} can manage post {action.Value.GetFullName()}");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<Post>> CheckCanAssignAsync(int postId, Post? post = null)
		{
			ActionResult<Post> action = new ActionResult<Post>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't check permissions. Unauthorized", eventId: EventIds.Unauthorized);

				if (post == null)
					post = await _db.Posts.FindAsync(postId);
				action.Value = post;

				if (action.Value == null)
					return action.FormFailure($"Can't check permissions. Post with ID {postId} not found", eventId: EventIds.NotFound);

				List<Post> actorHeads = Actor.GetPosts().SelectMany(p => p.GetAllHeadsRecursive()).ToList();

				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure($"{Actor.Nickname} don't have AssignPosts permission", eventId: EventIds.Forbidden);
				else if (!Actor.IsAdmin() && actorHeads.Contains(action.Value))
					return action.FormFailure($"Post {action.Value.GetFullName()} is one of {Actor.Nickname}'s heads", eventId: EventIds.Forbidden);

				action.FormSuccess($"{Actor.Nickname} can manage post {action.Value.GetFullName()}");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
	}
}
