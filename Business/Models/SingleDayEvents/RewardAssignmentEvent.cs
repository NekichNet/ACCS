using Business.Models.SingleDayEvents.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Business.Models.SingleDayEvents
{
	[Table("RewardAssignmentEvents")]
    public class RewardAssignmentEvent : EventWithDoc
    {
        [JsonIgnore] public virtual List<AssignedReward> AssignedRewards { get; set; }

        public override string GetHexColor()
        {
            return "#FFFF00";
        }

        public override string GetText()
        {
            string rewarded = Units.Count > 1 ? "награждены" : "награждён";
            return $"{string.Join(", ", Units.Select(u => u.Nickname))} {rewarded} {string.Join(", ", AssignedRewards.Select(ar => ar.Reward.Name))}";
        }
    }

	public class RewardAssignmentConfiguration : IEntityTypeConfiguration<RewardAssignmentEvent>
	{
		public void Configure(EntityTypeBuilder<RewardAssignmentEvent> builder)
		{
            builder.HasMany(e => e.AssignedRewards).WithOne(ra => ra.AssignmentEvent).OnDelete(DeleteBehavior.SetNull);
		}
	}
}
