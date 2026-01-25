using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace accs.Database
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
	{
		public AppDbContext CreateDbContext(string[] args)
		{
			Env.Load();
			string connectionString = Env.GetString("DB_CONNECTION_STRING")
				?? throw new InvalidOperationException("Connection string not configured for design time.");

			var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
			optionsBuilder
				.UseLazyLoadingProxies()
				.UseNpgsql(connectionString);

			return new AppDbContext(optionsBuilder.Options);
		}
	}
}
