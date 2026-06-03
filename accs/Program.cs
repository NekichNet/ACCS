using accs.Database;
using accs.Logging.Extensions;
using accs.Services;
using accs.Services.Interfaces;
using AspNet.Security.OAuth.Discord;
using DiscordOauth;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
using System.Text;


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

            var jwtSecret = Env.GetString("JWT_SECRET")
               ?? throw new InvalidOperationException("'JWT_SECRET' not configured in .env file");

            var jwtIssuer = Env.GetString("JWT_ISSUER") ?? "https://localhost:6001";
            var jwtAudience = Env.GetString("JWT_AUDIENCE") ?? "https://localhost:6001";
            var jwtExpiryMinutes = int.Parse(Env.GetString("JWT_EXPIRY_MINUTES") ?? "60");

            var key = Encoding.ASCII.GetBytes(jwtSecret);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IDiscordOAuthService, DiscordOAuthService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddControllers();

            _app = builder.Build();

            _app.UseAuthentication();
            _app.UseAuthorization();


            _app.Use(async (context, next) =>
            {
                var discordIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(discordIdClaim) &&
                    ulong.TryParse(discordIdClaim, out var discordId))
                {
                    using var scope = _app.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var unit = await db.Units.FindAsync(discordId);

                    if (unit != null)
                    {
                        context.Items["Actor"] = unit;
                    }
                }

                await next();
            });

            _app.MapControllers();
            _app.Run();
		}
	}
}
