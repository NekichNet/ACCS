using Business.Database;
using Business.Logging;
using Business.Models;
using Business.Models.Enums;
using Business.Models.Statuses;
using Business.Models.Util;
using Business.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
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
					return action.FormFailure("PostId creation failed. Can't get user's max available post", eventId: EventIds.NotFound);
				
				ActionResult<Rank> rankResult = await _rankService.GetAsync(maxRankId);
				if (!rankResult.IsSuccess)
					return action.FormFailure($"PostId creation failed. Rank with ID {maxRankId} not found", eventId: EventIds.NotFound);

				if (rank.GetAllHigherRecursive().Contains(rankResult.Value))
					return action.FormFailure($"PostId creation failed. Max post is higher, than user's max post", eventId: EventIds.Forbidden);

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

				action.FormSuccess($"PostId {action.Value.GetFullName()} created", eventId: EventIds.Created);
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
					action.FormSuccess("PostId found", eventId: EventIds.Read);
				else
					action.FormFailure("PostId not found", eventId: EventIds.NotFound);
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

				action.FormSuccess("PostId list formed, length: " + action.Value.Count(),
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
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
					return action.FormFailure($"PostId updating failed. Can't set head with ID {postId}", eventId: EventIds.Forbidden);

				if (subdivisionId != null)
				{
					_subdivisionService.Actor = Actor;
					ActionResult<Subdivision> subdivResult = await _subdivisionService.CheckCanManageAsync((int)subdivisionId);
					if (!subdivResult.IsSuccess)
						return action.FormFailure($"Can't assign post to subdivision with ID {subdivisionId}", eventId: EventIds.Forbidden);
				}

				Rank? rank = Actor.GetMaxRank();
				if (rank == null)
					return action.FormFailure("PostId updating failed. Can't get user's max available post", eventId: EventIds.NotFound);

				ActionResult<Rank> rankResult = await _rankService.GetAsync(maxRankId);
				if (!rankResult.IsSuccess)
					return action.FormFailure($"PostId updating failed. Rank with ID {maxRankId} not found", eventId: EventIds.NotFound);

				if (rank.GetAllHigherRecursive().Contains(rankResult.Value))
					return action.FormFailure($"PostId updating failed. Max post is higher, than user's max post", eventId: EventIds.Forbidden);

				result.Value.Name = name;
				result.Value.Description = description;
				result.Value.SubdivisionId = subdivisionId;
				result.Value.HeadId = headId;
				result.Value.MaxRankId = maxRankId;
				result.Value.Color = color;
				result.Value.AppendSubdivisionName = appendSubdivisionName;

				result.Value.UpdateRole();

				_db.Posts.Update(result.Value);
				await _db.SaveChangesAsync();

				action.FormSuccess("PostId updated", eventId: EventIds.Updated);
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
					return action.FormFailure("PostId updating. Permission check failed", eventId: EventIds.Forbidden);

				result.Value.UpdateRole();
				action.Value = result.Value.DiscordRoleId;

				await _db.SaveChangesAsync();

				action.FormSuccess($"PostId {result.Value.GetFullName()} Discord role updated", eventId: EventIds.Updated);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<Unit>>> GetUnitsByPostAsync(int postId)
		{
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				Post? post = await _db.Posts.FindAsync(postId);
				if (post == null)
					return action.FormFailure($"Getting units by post failed. Post with ID {postId} not found", eventId: EventIds.NotFound);

				action.Value = post.AssignedPosts.Where(r => r.IsActive()).Select(ar => ar.Unit).ToList();

				action.FormSuccess($"Units by {post.Name} post retrieved",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
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

				action.FormSuccess($"PostId {result.Value.Name} deleted", eventId: EventIds.Deleted);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<AssignedPost>>> SetPosts(ulong unitId, int[] postIds, int? docId = null)
		{
			ActionResult<List<AssignedPost>> action = new ActionResult<List<AssignedPost>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Posts setting restricted. Unauthorized", eventId: EventIds.Unauthorized);
				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure("Posts setting restricted", eventId: EventIds.Forbidden);

				var canAssignResult = await GetPostsCanAssignAsync();

				if (!canAssignResult.IsSuccess)
					return action.FormFailure("Posts setting failed. Unknown handled error", eventId: EventIds.HandledError);

				HashSet<Post> postsToAssign = new HashSet<Post>();
				HashSet<Post> postsToDepose = new HashSet<Post>();
				HashSet<Post> failedToAssign = new HashSet<Post>();
				HashSet<Post> failedToDepose = new HashSet<Post>();
				List<Post> canChange = canAssignResult.Value;

				if (!canChange.Any())
					return action.FormFailure("Posts setting failed. Can't set any posts", eventId: EventIds.Failed);

				foreach (int postId in postIds)
				{
					Post? postToAssign = await _db.Posts.FindAsync(postId);
					if (postToAssign == null)
						continue;
					if (!canChange.Contains(postToAssign))
					{
						failedToAssign.Add(postToAssign);
						continue;
					}
					postsToAssign.Add(postToAssign);
				}

				Unit? unit = await _db.Units.FindAsync(unitId);
				if (unit == null)
					return action.FormFailure($"Posts setting failed. Unit with ID {unitId} not found", eventId: EventIds.NotFound);
				if (!unit.IsActive() && !Actor.IsAdmin())
					return action.FormFailure($"Posts setting failed. Unit {unit.Nickname} is in retirement or dismissed", eventId: EventIds.Forbidden);

				List<AssignedPost> assignedPosts = unit.GetAssignedPosts();

				foreach (AssignedPost assignedPost in assignedPosts)
				{
					if (!canChange.Contains(assignedPost.Post))
					{
						failedToDepose.Add(assignedPost.Post);
						continue;
					}
					postsToDepose.Add(assignedPost.Post);
				}

				int postsInResult = assignedPosts.Count() + postsToAssign.Count() - postsToDepose.Count();
				if (postsInResult < 1)
					return action.FormFailure($"Posts setting failed. Posts in result cannot be under 1 ({postsInResult})",
						eventId: EventIds.ImpossibleAction);
				


				action.FormSuccess(
					$"",
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
		{
			ActionResult<AssignedPost> action = new ActionResult<AssignedPost>(_logger);

			try
			{
				ActionResult<Post> result = await CheckCanAssignPostAsync(postId);
				if (!result.IsSuccess)
					return action.FormFailure("PostId assigning restricted. Permission check failed", eventId: EventIds.Forbidden);

				if (unit == null)
					unit = await _db.Units.FindAsync(unitDiscordId);
				if (unit == null)
					return action.FormFailure($"PostId assigning failed. Unit with ID {unitDiscordId} not found", eventId: EventIds.NotFound);

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
				EmptyAction result = await CheckCanAssignPostAsync(postId, post);
				if (!result.IsSuccess)
					return action.FormFailure("Unit deposing restricted. Permission check failure", eventId: EventIds.Forbidden);

				if (unit == null)
					unit = await _db.Units.FindAsync(unitDiscordId);
				if (unit == null)
					return action.FormFailure($"Unit deposing failed. Unit with Discord ID {unitDiscordId} not found", eventId: EventIds.NotFound);
				if (!unit.IsActive())
					return action.FormFailure($"Unit {unit.Nickname} deposing impossible. Unit is in retirement or dismissed", eventId: EventIds.ImpossibleAction);
				if (unit.GetPosts().Count == 0)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit have no assigned postsToAssign", eventId: EventIds.ImpossibleAction);
				if (unit.GetPosts().Count == 1)
					return action.FormFailure($"Unit {unit.Nickname} deposing failed. Unit has last post assigned", eventId: EventIds.ImpossibleAction);
				
				if (assignedPost == null)
					assignedPost = await _db.AssignedPosts.FirstOrDefaultAsync(ap => ap.UnitId == unit.DiscordId && ap.PostId == postId && ap.IsActive(null));
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
				if (!Actor.HasPermission(PermissionType.ManageStructure))
					return action.FormFailure($"{Actor.Nickname} doesn't have ManageStructure permission", eventId: EventIds.Forbidden);

				if (post == null)
					post = await _db.Posts.FindAsync(postId);
				action.Value = post;

				if (action.Value == null)
					return action.FormFailure($"Can't check permissions. PostId with ID {postId} not found", eventId: EventIds.NotFound);

				List<Post> actorControllablePosts = Actor.GetPosts().SelectMany(p => p.GetAllSubordinatesRecursive()).ToList();
				
				if (!Actor.IsAdmin() && !actorControllablePosts.Contains(action.Value))
					return action.FormFailure($"PostId {action.Value.GetFullName()} isn't under {Actor.Nickname}'s control", eventId: EventIds.Forbidden);

				action.FormSuccess($"{Actor.Nickname} can manage post {action.Value.GetFullName()}");
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<Post>> CheckCanAssignPostAsync(int postId, Post? post = null)
		{
			ActionResult<Post> action = new ActionResult<Post>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't check post assignment permissions." +
						" Unauthorized", eventId: EventIds.Unauthorized);

				if (post == null)
					post = await _db.Posts.FindAsync(postId);
				action.Value = post;

				if (action.Value == null)
					return action.FormFailure($"Can't check post assignment permissions." +
						$" PostId with ID {postId} not found", eventId: EventIds.NotFound);

				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure($"{Actor.Nickname} don't have AssignPosts permission", eventId: EventIds.Forbidden);

				List<Post> actorHeads = Actor.GetPosts().SelectMany(p => p.GetAllHeadsRecursive()).ToList();

				if (!Actor.IsAdmin() && actorHeads.Contains(action.Value))
					return action.FormFailure(
						$"PostId {action.Value.GetFullName()} is one of {Actor.Nickname}'s heads", eventId: EventIds.Forbidden);

				action.FormSuccess($"{Actor.Nickname} can assign post {action.Value.GetFullName()}", eventId: EventIds.Accessed);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<Post>>> GetPostsCanAssignAsync()
		{
			ActionResult<List<Post>> action = new ActionResult<List<Post>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Getting postsToAssign can assign failed. Unauthorized", eventId: EventIds.Unauthorized);

				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure($"{Actor.Nickname} don't have AssignPosts permission", eventId: EventIds.Forbidden);

				if (Actor.IsAdmin())
				{
					action.Value = await _db.Posts.ToListAsync();
				}
				else
				{
					ActionResult<List<Post>> result = await GetAllNotHeadPostsAsync();
					if (!result.IsSuccess)
						return action.FormFailure("Getting postsToAssign can assign failed." +
							" Handled error in getting all not head postsToAssign", eventId: EventIds.HandledError);
					action.Value = result.Value;
				}

				action.FormSuccess($"{Actor.Nickname}'s postsToAssign can assign retrieved. Length: {action.Value.Count()}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		/*
		 * Какая-то чебухня. Не знаю, зачем я это накодил. Оно не корректно из-за текущей логики назначения должностей.
		 * Правильнее будет проверять именно по должностям, которые может снимать пользователь.
		public async Task<ActionResult<List<Unit>>> GetUnitsCanDeposeAsync()
		{
			ActionResult<List<Unit>> action = new ActionResult<List<Unit>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Getting units can change postsToAssign failed. Unauthorized", eventId: EventIds.Unauthorized);

				if (!Actor.HasPermission(PermissionType.AssignPosts))
					return action.FormFailure($"{Actor.Nickname} don't have AssignPosts permission", eventId: EventIds.Forbidden);

				if (Actor.IsAdmin())
				{
					action.Value = await _db.Units.ToListAsync();
				}
				else
				{
					_unitService.Actor = Actor;
					ActionResult<List<Unit>> result = await _unitService.GetAllNotHeadUnitsAsync();
					if (!result.IsSuccess)
						return action.FormFailure("Getting postsToAssign can assign failed." +
							" Handled error in getting all not head units", eventId: EventIds.HandledError);
					action.Value = result.Value;
				}

				action.FormSuccess($"{Actor.Nickname}'s postsToAssign can assign retrieved. Length: {action.Value.Count()}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
		*/

		public async Task<ActionResult<HashSet<Post>>> GetAllHeadPostsAsync()
		{
			ActionResult<HashSet<Post>> action = new ActionResult<HashSet<Post>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't get all head postsToAssign. Unauthorized", eventId: EventIds.Unauthorized);

				action.Value = Actor
					.GetPosts()
					.SelectMany(p => p.GetAllHeadsRecursive())
					.ToHashSet();

				action.FormSuccess($"{Actor.Nickname}'s all head postsToAssign retrieved. Length: {action.Value.Count}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<HashSet<Post>>> GetAllSubordinatePostsAsync()
		{
			ActionResult<HashSet<Post>> action = new ActionResult<HashSet<Post>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't get all subordinate postsToAssign. Unauthorized", eventId: EventIds.Unauthorized);

				action.Value = Actor
					.GetPosts()
					.SelectMany(p => p.GetAllSubordinatesRecursive())
					.ToHashSet();

				action.FormSuccess($"{Actor.Nickname}'s all not subordinate retrieved. Length: {action.Value.Count}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}

		public async Task<ActionResult<List<Post>>> GetAllNotHeadPostsAsync()
		{
			ActionResult<List<Post>> action = new ActionResult<List<Post>>(_logger);

			try
			{
				if (Actor == null)
					return action.FormFailure("Can't get all not head postsToAssign. Unauthorized", eventId: EventIds.Unauthorized);

				HashSet<Post> headPosts = Actor
					.GetPosts()
					.SelectMany(p => p.GetAllHeadsRecursive())
					.ToHashSet();

				action.Value = await _db.Posts.Except(headPosts).ToListAsync();

				action.FormSuccess($"{Actor.Nickname}'s all not head postsToAssign retrieved. Length: {action.Value.Count}",
					eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
			}
			catch (Exception ex)
			{
				action.FormException(ex);
			}

			return action;
		}
	}
}
