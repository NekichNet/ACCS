using accs.Database;
using accs.Logging.Extensions;
using AspNet.Security.OAuth.Discord;
using DiscordOauth;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;


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

			/*
			 * Дениска, разберись с этой discord oauth2.0 хунёй
			 * 
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			}).AddDiscord(options =>
			{
				var oauthProviders = builder.Configuration.GetSection("OAuthProviders").Get<OAuthProviders>();
				if (oauthProviders is null) throw new InvalidOperationException("OAuthProviders is not configured");

				var discordOptions = oauthProviders.Providers["Discord"];
				if (discordOptions is null) throw new InvalidOperationException("Discord OAuth provider is not configured");

				options.ClientId = discordOptions.ClientId;
				options.ClientSecret = discordOptions.ClientSecret;
				options.CallbackPath = discordOptions.CallBack;
				options.SaveTokens = true;

				options.CorrelationCookie.SameSite = SameSiteMode.Lax;
				options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

				options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
					string.Format(
						CultureInfo.InvariantCulture,
						"https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
						user.GetString("id"),
						user.GetString("avatar"),
						user.GetString("avatar")!.StartsWith("a_") ? "gif" : "png"));

				options.Scope.Add("identify");
				options.Scope.Add("email");
			}).AddCookie(options =>
			{
				options.Cookie.Name = "DiscordAuth";
				options.LoginPath = "/login";
				options.LogoutPath = "/logout";
				options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
				options.Cookie.SameSite = SameSiteMode.Lax;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			}).AddJwtBearer(options =>
			{
				var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
				if (jwtOptions is null) throw new InvalidOperationException("JwtOptions is not configured");

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtOptions.Issuer,
					ValidAudience = jwtOptions.Audience,
					IssuerSigningKey = new RsaSecurityKey(LoadRsaKey(jwtOptions.RsaPublicKeyLocation)),
					RequireSignedTokens = true
				};
			});
			*/

			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

			_app = builder.Build();

			_app.Run();
		}
	}
}
