using accs.Models.SingleDayEvents.Abstraction;
using accs.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.SingleDayEvents
{
	[Table("RewardAssignmentEvents")]
    public class RewardAssignmentEvent : EventWithInitiator
    {
        public int AssignedRewardId { get; set; }
        [JsonIgnore] public virtual AssignedReward AssignedReward { get; set; }

        public override string GetHexColor()
        {
            return AssignedReward.Reward.Color;
        }

        public override string GetText()
        {
            throw new NotImplementedException();
        }
    }

	public class RewardAssignmentConfiguration : IEntityTypeConfiguration<RewardAssignmentEvent>
	{
		public void Configure(EntityTypeBuilder<RewardAssignmentEvent> builder)
		{
            builder.HasOne(e => e.AssignedReward).WithOne(ra => ra.AssignmentEvent).OnDelete(DeleteBehavior.SetNull);
		}
	}
}
