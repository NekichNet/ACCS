
using accs.Repository;
using accs.Repository.Context;
using accs.Repository.Interfaces;
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
            builder.Services.AddScoped<IStatusRepository, StatusRepository>();
			builder.Services.AddScoped<ITicketRepository, TicketRepository>();

			var discordConfig = new DiscordSocketConfig() { };

            builder.Services.AddSingleton(discordConfig);
            builder.Services.AddSingleton<DiscordSocketClient>();
            builder.Services.AddSingleton<IOCRService, OCRService>();

			//builder.Services.AddTransient<VoiceChannelsModule>();

			var app = builder.Build();

			string token = Env.GetString("TOKEN", "Token not found");
			if (token == "Token not found") { Console.WriteLine("Token not found"); return; }

			string guildIdString = Env.GetString("SERVER_ID", "Server id not found");
			ulong guildId;
			if (!ulong.TryParse(guildIdString, out guildId)) { Console.WriteLine("Cannot parse guild id!"); return; }

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

			//interaction.RemoveModulesFromGuildAsync(guildId);

			//interaction.RemoveModuleAsync<ActivityGroupModule>().Wait();
			//interaction.RemoveModuleAsync<LogsGroupModule>().Wait();
			//interaction.RemoveModuleAsync<PostAssignmentModule>().Wait();
			//interaction.RemoveModuleAsync<ProfileGroupModule>().Wait();
			//interaction.RemoveModuleAsync<RankAssignmentModule>().Wait();
			//interaction.RemoveModuleAsync<RewardGroupModule>().Wait();
			//interaction.RemoveModuleAsync<StatusAssignmentModule>().Wait();
			//interaction.RemoveModuleAsync<TicketGroupModule>().Wait();
			//interaction.RemoveModuleAsync<TicketMessageHandler>().Wait();
			//interaction.RemoveModuleAsync<VoiceChannelsModule>().Wait();

			//interaction.AddModuleAsync<ActivityGroupModule>(app.Services).Wait();
			//interaction.AddModuleAsync<LogsGroupModule>(app.Services).Wait();
			//interaction.AddModuleAsync<PostAssignmentModule>(app.Services).Wait();
			//interaction.AddModuleAsync<ProfileGroupModule>(app.Services).Wait();
			//interaction.AddModuleAsync<RankAssignmentModule>(app.Services).Wait();
			//interaction.AddModuleAsync<RewardGroupModule>(app.Services).Wait();
			//interaction.AddModuleAsync<StatusAssignmentModule>(app.Services).Wait();
			//interaction.AddModuleAsync<TicketGroupModule>(app.Services).Wait();
			//interaction.AddModuleAsync<TicketMessageHandler>(app.Services).Wait();
			//interaction.AddModuleAsync<VoiceChannelsModule>(app.Services).Wait();

			//interaction.AddModulesToGuildAsync();

			client.InteractionCreated += async (msg) =>
			{
				var ctx = new SocketInteractionContext(client, msg);
				await interaction.ExecuteCommandAsync(ctx, app.Services);
			};

			client.Ready += async Task () =>
            {
                Console.WriteLine("Client is ready");
				interaction.AddModulesAsync(Assembly.GetEntryAssembly(), app.Services).Wait();
				interaction.RegisterCommandsToGuildAsync(guildId).Wait();
                Console.WriteLine("Commands registered");
			};

			client.StartAsync().Wait();

			app.Run();
		}
    }
}
