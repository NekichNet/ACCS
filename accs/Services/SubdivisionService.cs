using accs.Database;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Util;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
    public class SubdivisionService : BusinessService
    {
        private readonly AppDbContext _db;

        public SubdivisionService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Subdivision>> CreateAsync(string name, string? envRoleString = null, int? headId = null)
        {
            ActionResult<Subdivision> action = new ActionResult<Subdivision>(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        action.Value = new Subdivision(name, envRoleString)
                        {
                            HeadId = headId
                        };

                        action.Value.UpdateRole();

                        await _db.Subdivisions.AddAsync(action.Value);
                        await _db.SaveChangesAsync();

                        action.FormSuccess("Subdivision created");
                    }
                    else
                    {
                        action.FormFailure("Subdivision creation restricted");
                    }
                }
                else
                {
                    action.FormFailure("Subdivision creation restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Subdivision>> GetAsync(int id)
        {
            ActionResult<Subdivision> action = new ActionResult<Subdivision>(_logger);

            try
            {
                action.Value = await _db.Subdivisions.FindAsync(id);
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
                action.FormSuccess("Subdivision list formed, length: " + action.Value.Count());
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(
            int id,
            string name,
            string? color = null,
            int? headId = null
            )
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var subdivision = await _db.Subdivisions.FindAsync(id);
                        if (subdivision != null)
                        {
                            subdivision.Name = name;
                            if (color != null)
                                subdivision.Color = color;
                            if (headId.HasValue)
                                subdivision.HeadId = headId.Value;

                            _db.Subdivisions.Update(subdivision);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Subdivision updated");
                        }
                        else
                        {
                            action.FormFailure("Subdivision not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Subdivision update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Subdivision update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateRoleAsync(int id)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var subdivision = await _db.Subdivisions.FindAsync(id);
                        if (subdivision != null)
                        {
                            subdivision.UpdateRole();
                            _db.Subdivisions.Update(subdivision);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Subdivision discord role updated");
                        }
                        else
                        {
                            action.FormFailure("Subdivision not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Subdivision role update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Subdivision role update restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }
    }
}