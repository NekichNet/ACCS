using Business.Models.Abstraction;
using Business.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Models
{
	public class Reward : IEntityWithDiscordRole, IEntityWithFiles
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Color { get; set; } = "#FFFFFF";
		public string Conditions { get; set; } = string.Empty;
		public string Privileges { get; set; } = string.Empty;
		public bool CanBeAssigned { get; set; } = true;
		public ulong? DiscordRoleId { get; set; }
		[JsonIgnore] public virtual List<AssignedReward> AssignedRewards { get; set; } = new List<AssignedReward>();

		public void UpdateRole()
		{
			if (DiscordRoleId != null)
			{
				// TODO: Send request to discord-bot api
			}
		}

		public void CheckRoleOnUser(ulong unitId)
		{
			if (DiscordRoleId != null)
			{
				// TODO: Send request to discord-bot api
			}
		}

		public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public string GetFilesFolderName()
        {
            return "rewards";
        }
    }

	public class RewardDto
	{
		public string Name { get; set; } = string.Empty;
		public string Color { get; set; } = "#FFFFFF";
		public string Conditions { get; set; } = string.Empty;
		public string Privileges { get; set; } = string.Empty;
	}

	public class RewardConfiguration : IEntityTypeConfiguration<Reward>
	{
		public void Configure(EntityTypeBuilder<Reward> builder)
		{
			builder.HasMany(r => r.AssignedRewards).WithOne(ar => ar.Reward).HasForeignKey(ar => ar.RewardId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
