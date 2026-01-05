using accs.Models;
using Microsoft.EntityFrameworkCore;

namespace accs.Repository.Context
{
    public class AppDbContext : DbContext
    {
		public DbSet<Unit> Units { get; set; }
		public DbSet<Post> Posts { get; set; }
		public DbSet<Rank> Ranks { get; set; }
		public DbSet<Subdivision> Subdivisions { get; set; }
		public DbSet<Permission> Permissions { get; set; }
		public DbSet<RewardType> RewardTypes { get; set; }
		public DbSet<Reward> Rewards { get; set; }
		public DbSet<DocType> DocTypes { get; set; }
		public DbSet<Doc> Docs { get; set; }

		public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options) { }
	}
}
