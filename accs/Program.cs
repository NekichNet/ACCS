using accs.DiscordBot.Interactions;
using accs.DiscordBot.Interactions.accs.DiscordBot.Interactions;
using accs.Repository;
using accs.Repository.Context;
using accs.Repository.Interfaces;
using accs.Services;
using accs.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
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
			client.StartAsync().Wait();

			InteractionService interaction = new InteractionService(client);

            interaction.AddModuleAsync<ActivityGroupModule>(app.Services).Wait();
			interaction.AddModuleAsync<LogsGroupModule>(app.Services).Wait();
			interaction.AddModuleAsync<PostAssignmentModule>(app.Services).Wait();
			interaction.AddModuleAsync<ProfileGroupModule>(app.Services).Wait();
			interaction.AddModuleAsync<RankAssignmentModule>(app.Services).Wait();
			interaction.AddModuleAsync<RewardGroupModule>(app.Services).Wait();
			interaction.AddModuleAsync<StatusAssignmentModule>(app.Services).Wait();
			interaction.AddModuleAsync<TicketGroupModule>(app.Services).Wait();
			interaction.AddModuleAsync<TicketMessageHandler>(app.Services).Wait();
			interaction.AddModuleAsync<VoiceChannelsModule>(app.Services).Wait();

			client.Ready += async Task () =>
            {
                Console.WriteLine("Client is ready");
                interaction.RegisterCommandsToGuildAsync(guildId).Wait();
                Console.WriteLine("Commands registered");
			};

			app.Run();
		}
    }
}
