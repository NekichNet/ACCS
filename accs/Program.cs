using accs.Database;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

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

			/*
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
            builder.Services.AddScoped<IStatusRepository, StatusRepository>();
			builder.Services.AddScoped<ITicketRepository, TicketRepository>();
			*/

			var discordConfig = new DiscordSocketConfig() { };

            builder.Services.AddSingleton(discordConfig);
            builder.Services.AddSingleton<DiscordSocketClient>();
			builder.Services.AddSingleton<IGuildProviderService, GuildProviderService>();

			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

			builder.Services.AddSingleton<IOCRService, OCRService>();
			builder.Services.AddScoped<ILogService, LogService>();
			builder.Services.AddTransient<IUsersCleanUpService, UsersCleanupService>();

			var app = builder.Build();

			string token = Env.GetString("TOKEN", "Token not found");
			if (token == "Token not found") { Console.WriteLine("Token not found"); return; }

			DiscordSocketClient client = app.Services.GetRequiredService<DiscordSocketClient>();

			client.Log += async (msg) =>
			{
				await Task.CompletedTask;
				Console.WriteLine(msg);
			};
			
			client.LoginAsync(TokenType.Bot, token).Wait();
			
			InteractionService interaction = new InteractionService(client.Rest);

			interaction.Log += async (msg) =>
			{
				await Task.CompletedTask;
				Console.WriteLine(msg);
			};
			
			client.InteractionCreated += async (msg) =>
			{
				var ctx = new SocketInteractionContext(client, msg);
				await interaction.ExecuteCommandAsync(ctx, app.Services);
			};

			IGuildProviderService guildProvider = app.Services.GetRequiredService<IGuildProviderService>();

			client.Ready += async Task () =>
            {
                Console.WriteLine("Client is ready");

				SocketGuild guild = guildProvider.GetGuild();
				if (!guild.IsConnected)
					throw new Exception("Client is not connected to guild!");

				
				// Очищаем уже зарегистрированные команды
				await client.Rest.BulkOverwriteGlobalCommands(new ApplicationCommandProperties[] { });
				await client.Rest.BulkOverwriteGuildCommands(new ApplicationCommandProperties[] { }, guildProvider.GetGuildId());
				
				
				// Регистрируем актуальные команды
				await interaction.AddModulesAsync(Assembly.GetEntryAssembly(), app.Services);
				await interaction.RegisterCommandsToGuildAsync(guildProvider.GetGuildId());

				Console.WriteLine("Commands registered");
			};

			client.StartAsync().Wait();

			app.Run();
		}
    }
}
