using accs.Repository;
using accs.Repository.Context;
using accs.Repository.Interfaces;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

namespace accs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.Load(".env");

            var connectionString = Env.GetString("DB_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Connection string not configured.");

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddTransient<ILogService, LogService>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
            builder.Services.AddScoped<IDocRepository, DocRepository>();
            builder.Services.AddScoped<IDocTypeRepository, DocTypeRepository>();
            builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();
            builder.Services.AddScoped<IRankRepository, RankRepository>();
            builder.Services.AddScoped<IRewardRepository, RewardRepository>();
            builder.Services.AddScoped<ISubdivisionRepository, SubdivisionRepository>();
            builder.Services.AddScoped<IUnitStatusRepository, UnitStatusRepository>();
            builder.Services.AddScoped<IUnitRepository, UnitRepository>();

			var discordConfig = new DiscordSocketConfig() { };

            builder.Services.AddSingleton(discordConfig);
            builder.Services.AddSingleton<DiscordSocketClient>();
            builder.Services.AddSingleton<IDiscordBotService, DiscordBotService>();

			var app = builder.Build();

            app.Run();
        }
    }
}
