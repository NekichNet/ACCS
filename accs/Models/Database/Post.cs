using accs.Database;
using accs.Models.Database.Configurations;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace accs.Models.Database
{
	[EntityTypeConfiguration(typeof(PostConfiguration))]
	public class Post : PermissionCheckable
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public int? SubdivisionId { get; set; }
		public virtual Subdivision? Subdivision { get; set; }
		public ulong? DiscordRoleId { get; set; }
		public bool AppendSubdivisionName { get; set; } = false;
		public int? HeadId{ get; set; }
		public virtual Post? Head { get; set; }
		public virtual List<Post> Subordinates { get; set; } = new List<Post>();
		public virtual List<Unit> Units { get; set; } = new List<Unit>();

		public Post(string envRoleString)
		{
			DiscordRoleId = ulong.Parse(DotNetEnv.Env.GetString(envRoleString, $"{envRoleString} Not found"));
		}

		public Post() { }

		public string GetFullName()
		{
			return Subdivision != null && AppendSubdivisionName ? Name + " " + Subdivision.GetFullName() : Name;
		}

		public List<Post> GetAllSubordinatesRecursive()
		{
			List<Post> result = [.. Subordinates];

			foreach (Post sub in Subordinates)
			{
				result.AddRange(sub.GetAllSubordinatesRecursive());
			}

			return result;
		}

		public List<Post> GetAllHeadsRecursive()
		{
			List<Post> result = new List<Post>();
			Post? tempHead = Head;

			while (tempHead != null)
			{
				result.Add(tempHead);
				tempHead = tempHead.Head;
			}

			return result;
		}

		public Subdivision? GetHighestLevelSubdivision()
		{
			Subdivision? currentSubdivision = Subdivision;

			if (currentSubdivision != null)
				while (currentSubdivision.Head != null)
					currentSubdivision = currentSubdivision.Head;

			return currentSubdivision;
		}

		public override string ToString()
		{
			return Id.ToString() + " " + Name;
		}

		public async Task NotifyOnAssignAsync(SocketGuild guild, AppDbContext db, Unit unit, ulong? channelId = null)
		{
			try
			{
				if (DiscordNotification != null)
				{
					string text = "";

					Dictionary<string, string> replaces = new Dictionary<string, string>();

					if (DiscordNotification.AuthorId != null)
					{
						SocketGuildUser authorUser = guild.GetUser((ulong)DiscordNotification.AuthorId);
						if (authorUser != null)
						{
							replaces.Add("<AuthorMention>", authorUser.Mention);
						}

						Unit? authorUnit = await db.Units.FindAsync(DiscordNotification.AuthorId);
						if (authorUnit != null)
						{
							replaces.Add("<AuthorName>", authorUnit.GetOnlyNickname());
							replaces.Add("<AuthorRank>", authorUnit.Rank.Name);
							if (authorUnit.Posts.Any())
								replaces.Add("<AuthorPost>", authorUnit.Posts.OrderByDescending(p => p.Permissions.Count).First().GetFullName());
						}
						else
						{
							if (authorUser != null)
							{
								replaces.Add("<AuthorName>", authorUser.DisplayName);
							}
						}
					}

					SocketGuildUser user = guild.GetUser(unit.DiscordId);

					if (user != null)
					{
						text += user.Mention;
						replaces.Add("<UnitMention>", user.Mention);
					}

					replaces.Add("<UnitName>", unit.GetOnlyNickname());
					replaces.Add("<UnitRank>", unit.Rank.Name);
					replaces.Add("<Post>", GetFullName());
					replaces.Add("<PostDescription>", Description);

					if (DiscordRoleId != null)
					{
						SocketRole postRole = guild.GetRole((ulong)DiscordRoleId);
						if (postRole != null)
						{
							replaces.Add("<PostMention>", postRole.Mention);
						}
					}

					if (Subdivision != null)
					{
						replaces.Add("<SubdivisionName>", Subdivision.GetFullName());
						if (Subdivision.DiscordRoleId != null)
						{
							SocketRole subdivisionRole = guild.GetRole((ulong)Subdivision.DiscordRoleId);
							if (subdivisionRole != null)
							{
								text += subdivisionRole.Mention;
								replaces.Add("<SubdivisionMention>", subdivisionRole.Mention);
							}
						}
					}

					DiscordNotification newNotification = DiscordNotification.ApplyReplace(replaces);

					EmbedBuilder embed = new EmbedBuilder()
						.WithDescription(newNotification.Text)
						.WithFooter(newNotification.Footer)
						.WithColor(DiscordNotification.GetEmbedColor());

					//if (DiscordNotification.AuthorId != null)
					//{
					//	SocketGuildUser authorUser = guild.GetUser((ulong)DiscordNotification.AuthorId);
					//	if (authorUser != null)
					//	{
					//		embed.WithAuthor(authorUser);
					//	}
					//}

					if (user != null)
					{
						embed.WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
					}

					List<string> imgUrls = DiscordNotification.Images.Split(";").ToList();
					if (imgUrls.Any())
					{
						embed.WithImageUrl(imgUrls.Shuffle().First());
					}

					SocketTextChannel channel = guild.GetTextChannel(channelId == null ? DiscordNotification.ChannelId : (ulong)channelId);

					RestUserMessage message = await channel.SendMessageAsync(
						embed: embed.Build(),
						allowedMentions: AllowedMentions.All
					);

					if (!Directory.Exists("temp"))
						Directory.CreateDirectory("temp");

					string filePath = Path.Join("temp", $"notification-{message.Id}.txt");
					using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
					{
						await writer.WriteAsync(newNotification.Shortened);
					}

					ButtonBuilder button = new ButtonBuilder()
						.WithCustomId($"hide:{unit.DiscordId},{DiscordNotification.Id},{message.Id}")
						.WithLabel("Скрыть")
						.WithStyle(ButtonStyle.Secondary);

					ComponentBuilder component = new ComponentBuilder()
						.WithButton(button);

					await channel.SendMessageAsync(
						text: text,
						allowedMentions: AllowedMentions.All,
						components: component.Build()
					);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Произошла ошибка в NotifyOnAssignAsync: " + e.StackTrace);
			}
		}

		public override HashSet<Permission> GetPermissionsRecursive()
		{
			HashSet<Permission> permissions = [.. GetPermissions()];
			if (Subdivision != null)
				permissions.Concat(Subdivision.GetPermissionsRecursive());
			permissions.Concat(Subordinates.SelectMany(
				s => s.GetGivedPermissionsRecursive()
					.Where(gp => gp.Inherit)
					.Select(gp => gp.Permission)
				));
			return permissions;
		}

		public override HashSet<GivedPermission> GetGivedPermissionsRecursive()
		{
			throw new NotImplementedException();
		}
	}
}
