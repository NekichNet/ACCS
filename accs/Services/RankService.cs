
using accs.Database;
using accs.Models;
using accs.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
    public class RankService : BusinessService
    {
        private readonly AppDbContext _db;
        public RankService(AppDbContext db, ILogger logger) : base(logger)
        {
            _db = db;
        }

        public async Task<ActionResult<Rank>> CreateAsync(int id, string name)
        {
            ActionResult<Rank> action = new ActionResult<Rank>(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        action.Value = new Rank(id, name);

                        await _db.Ranks.AddAsync(action.Value);
                        await _db.SaveChangesAsync();

                        action.FormSuccess("Rank created");
                    }
                    else
                    {
                        action.FormFailure("Rank creation restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank creation restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<Rank>> GetAsync(int id)
        {
            ActionResult<Rank> action = new ActionResult<Rank>(_logger);

            try
            {
                action.Value = await _db.Ranks.FindAsync(id);
                if (action.Value != null)
                    action.FormSuccess("Rank found");
                else
                    action.FormFailure("Rank not found");
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<ActionResult<List<Rank>>> GetAllAsync()
        {
            ActionResult<List<Rank>> action = new ActionResult<List<Rank>>(_logger);

            try
            {
                action.Value = await _db.Ranks.ToListAsync();
                action.FormSuccess("Rank list formed, length: " + action.Value.Count());
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> DeleteAsync(int id)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var rank = await _db.Ranks.FindAsync(id);
                        if (rank != null)
                        {
                            _db.Ranks.Remove(rank);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Rank deleted");
                        }
                        else
                        {
                            action.FormFailure("Rank not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Rank deletion restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank deletion restricted. Unauthorized");
                }
            }
            catch (Exception ex)
            {
                action.FormException(ex);
            }

            return action;
        }

        public async Task<EmptyAction> UpdateAsync(int id, string name)
        {
            EmptyAction action = new EmptyAction(_logger);

            try
            {
                if (Actor != null)
                {
                    if (Actor.HasPermission(PermissionType.ManageStructure))
                    {
                        var rank = await _db.Ranks.FindAsync(id);
                        if (rank != null)
                        {
                            rank.Name = name;
                            _db.Ranks.Update(rank);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Rank updated");
                        }
                        else
                        {
                            action.FormFailure("Rank not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Rank update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank update restricted. Unauthorized");
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
                        var rank = await _db.Ranks.FindAsync(id);
                        if (rank != null)
                        {
                            rank.UpdateRole();
                            _db.Ranks.Update(rank);
                            await _db.SaveChangesAsync();
                            action.FormSuccess("Rank discord role updated");
                        }
                        else
                        {
                            action.FormFailure("Rank not found");
                        }
                    }
                    else
                    {
                        action.FormFailure("Rank role update restricted");
                    }
                }
                else
                {
                    action.FormFailure("Rank role update restricted. Unauthorized");
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