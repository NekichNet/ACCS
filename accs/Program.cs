using accs.Database;
using accs.Logging;
using accs.Logging.Extensions;
using accs.Models.Enums;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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

			builder.Logging.ClearProviders();
			builder.Logging.AddCustomConsole();
			builder.Logging.AddFile();

			builder.Services.AddSingleton(discordConfig);
            builder.Services.AddSingleton<DiscordSocketClient>();
			builder.Services.AddSingleton<IGuildProviderService, GuildProviderService>();

			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

			builder.Services.AddScoped<IOCRService, OCRService>();

			//builder.Services.AddTransient<IUsersCleanUpService, UsersCleanupService>();

			//builder.Services.AddHostedService<DailyCleanupService>();
			builder.Services.AddHostedService<AutoStatusService>();

			_app = builder.Build();

			string token = Env.GetString("TOKEN", "Token not found");
			if (token == "Token not found") { Console.WriteLine("Token not found"); return; }

			DiscordSocketClient client = _app.Services.GetRequiredService<DiscordSocketClient>();

			client.Log += DiscordLog<DiscordSocketClient>;

			client.LoginAsync(TokenType.Bot, token).Wait();
			
			InteractionService interaction = new InteractionService(client.Rest);

			interaction.Log += DiscordLog<InteractionService>;

			client.InteractionCreated += async (msg) =>
			{
				var ctx = new SocketInteractionContext(client, msg);
				await interaction.ExecuteCommandAsync(ctx, _app.Services);
			};

			IGuildProviderService guildProvider = _app.Services.GetRequiredService<IGuildProviderService>();
			ILogger<DiscordSocketClient> clientLogger = _app.Services.GetRequiredService<ILogger<DiscordSocketClient>>();

			client.Ready += async Task () =>
            {
				clientLogger.LogInformation(EventIds.Ok, "Client is ready");

				SocketGuild guild = guildProvider.GetGuild();
				if (!guild.IsConnected)
				{
					Exception ex = new Exception("Client is not connected to guild!");
					clientLogger.LogCritical(EventIds.UnhandledError, ex, ex.Message);
				}

				/*
				// Очищаем уже зарегистрированные команды
				await client.Rest.BulkOverwriteGlobalCommands(new ApplicationCommandProperties[] { });
				await client.Rest.BulkOverwriteGuildCommands(new ApplicationCommandProperties[] { }, guildProvider.GetGuildId());
				clientLogger.LogInformation(EventIds.Deleted, "Commands vanished");
				*/

				// Регистрируем актуальные команды
				await interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _app.Services);
				await interaction.RegisterCommandsToGuildAsync(guildProvider.GetGuildId());

				clientLogger.LogInformation(EventIds.Created, "Commands registered");
			};

			client.StartAsync().Wait();

			_app.Run();
		}

		private async static Task DiscordLog<TCategoryName>(LogMessage message)
		{
			LogLevel logLevel = (LogLevel)(5 - message.Severity);
			_app.Services.GetRequiredService<ILogger<TCategoryName>>()
				.Log(logLevel, message.Exception, message.Message);
		}
	}
}
