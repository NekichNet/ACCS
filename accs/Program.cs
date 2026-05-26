using accs.Database;
using accs.Logging.Extensions;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

namespace accs
{
    public class Program
    {
		private static WebApplication _app;

        public static void Main(string[] args)
        {
            Env.Load(".env");

            var connectionString = Env.GetString("DB_CONNECTION_STRING")
				?? throw new InvalidOperationException("Connection string not configured.");

            var builder = WebApplication.CreateBuilder(args);

			builder.Logging.ClearProviders();
			builder.Logging.AddCustomConsole();
			builder.Logging.AddFile();

			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

			_app = builder.Build();

			_app.Run();
		}
	}
}
