using Business.Database;
using Business.Logging;
using Business.Models;
using Business.Models.Enums;
using Business.Models.Util;
using Business.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class SubdivisionService : BusinessService
    {
        private readonly AppDbContext _db;

        public SubdivisionService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Subdivision>> CreateAsync(
            string name,
            bool appendSubdivisionName,
            string description,
            string color,
            ulong? discordRoleId,
            int? headId
            )
        {
            ActionResult<Subdivision> action = new ActionResult<Subdivision>(_logger);

            try
            {
                if (headId != null)
                {
                    if (Actor == null)
                        return action.FormFailure("Creating subdivision restricted. Unauthorized", eventId: EventIds.Unauthorized);
                    if (!Actor.HasPermission(PermissionType.ManageStructure))
                        return action.FormFailure("Creating subdivision restricted", eventId: EventIds.Forbidden);
                }
                else
                {
                    ActionResult<Subdivision> result = await CheckCanManageAsync((int)headId);
                    if (!result.IsSuccess)
                        return action.FormFailure("Permission check failed");
                }

                action.Value = new Subdivision
                {
                    Name = name,
                    AppendHeadName = appendSubdivisionName,
                    Description = description,
                    Color = color,
                    DiscordRoleId = discordRoleId,
                    HeadId = headId
                };

                action.Value.UpdateRole();

                await _db.Subdivisions.AddAsync(action.Value);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Subdivision {action.Value.GetFullName()} created");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Subdivision>> GetAsync(int subdivisionId)
        {
            ActionResult<Subdivision> action = new ActionResult<Subdivision>(_logger);

            try
            {
                action.Value = await _db.Subdivisions.FindAsync(subdivisionId);
                if (action.Value != null)
                    action.FormSuccess("Subdivision found");
                else
                    action.FormFailure("Subdivision not found");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<Subdivision>>> GetAllAsync()
        {
            ActionResult<List<Subdivision>> action = new ActionResult<List<Subdivision>>(_logger);

            try
            {
                action.Value = await _db.Subdivisions.ToListAsync();

                action.FormSuccess("Subdivision list formed, length: " + action.Value.Count(),
                    eventId: action.Value.Count() > 0 ? EventIds.Read : EventIds.NoData);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(
            int subdivisionId,
            string name,
            string? color = null,
            int? headId = null
            )
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                ActionResult<Subdivision> result = await CheckCanManageAsync(subdivisionId);

                if (!result.IsSuccess)
                    return action.FormFailure("Permission check failed");

                result.Value.Name = name;
                if (color != null)
                    result.Value.Color = color;
                if (headId.HasValue)
                    result.Value.HeadId = headId.Value;

                result.Value.UpdateRole();

                _db.Subdivisions.Update(result.Value);
                await _db.SaveChangesAsync();

                action.FormSuccess($"Subdivision {result.Value} updated", eventId: EventIds.Updated);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        /// <summary>
        /// Обновляет Discord роль подразделения или создаёт её, если не существует
        /// </summary>
        /// <param name="subdivisionId">ID подразделения</param>
        /// <returns>ActionResult с Discord ID роли подразделения</returns>
        public async Task<ActionResult<ulong?>> UpdateRoleAsync(int subdivisionId)
        {
            ActionResult<ulong?> action = new ActionResult<ulong?>(_logger);

            try
            {
                ActionResult<Subdivision> result = await CheckCanManageAsync(subdivisionId);

                if (!result.IsSuccess)
                    return action.FormFailure("Permission check failed");

                result.Value.UpdateRole();
                action.Value = result.Value.DiscordRoleId;

                await _db.SaveChangesAsync();

                action.FormSuccess($"Subdivision {result.Value.GetFullName()} Discord role updated");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> DeleteAsync(int subdivisionId)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                ActionResult<Subdivision> result = await CheckCanManageAsync(subdivisionId);

                if (!result.IsSuccess)
                    return action.FormFailure("Permission check failed", eventId: EventIds.Forbidden);

                _db.Subdivisions.Remove(result.Value);

                await _db.SaveChangesAsync();

                action.FormSuccess($"Subdivision {result.Value.Name} deleted", eventId: EventIds.Deleted);
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Subdivision>> CheckCanManageAsync(int subdivisionId)
        {
            ActionResult<Subdivision> action = new ActionResult<Subdivision>(_logger);

            try
            {
                if (Actor == null)
                    return action.FormFailure("Can't check permissions. Unauthorized", eventId: EventIds.Unauthorized);

                action.Value = await _db.Subdivisions.FindAsync(subdivisionId);

                if (action.Value == null)
                    return action.FormFailure($"Can't check permissions. Subdivision with ID {subdivisionId} not found", eventId: EventIds.NotFound);

                List<Post> actorControllablePosts = Actor.GetPosts().SelectMany(p => p.GetAllSubordinatesRecursive()).ToList();

                if (!Actor.HasPermission(PermissionType.ManageStructure))
                    return action.FormFailure($"{Actor.Nickname} don't have ManageStructure permission", eventId: EventIds.Forbidden);
                else if (!Actor.IsAdmin() && action.Value.Posts.Any(p => !actorControllablePosts.Contains(p)))
                    return action.FormFailure($"Subdivision {action.Value.GetFullName()} is not under {Actor.Nickname}'s control", eventId: EventIds.Forbidden);

                action.FormSuccess($"{Actor.Nickname} can manage subdivision {action.Value.GetFullName()}");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }
    }
}