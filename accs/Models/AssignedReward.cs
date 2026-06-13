using accs.Models.SingleDayEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	public class AssignedReward
    {
        public int RewardId { get; set; }
		[JsonIgnore] public virtual Reward Reward { get; set; }
        public ulong UnitId { get; set; }
        [JsonIgnore] public virtual Unit Unit { get; set; }
		public int? AssignmentEventId { get; set; }
		[JsonIgnore] public virtual RewardAssignmentEvent? AssignmentEvent { get; set; }
		public bool Display { get; set; }

		public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

	public class AssignedRewardConfiguration : IEntityTypeConfiguration<AssignedReward>
	{
		public void Configure(EntityTypeBuilder<AssignedReward> builder)
		{
			builder.HasKey(ar => new { ar.RewardId, ar.UnitId });
			builder.HasOne(ar => ar.Reward).WithMany(r => r.Assigned).HasForeignKey(ar => ar.RewardId).OnDelete(DeleteBehavior.SetNull);
			builder.HasOne(ar => ar.Unit).WithMany(u => u.AssignedRewards).HasForeignKey(ar => ar.UnitId).OnDelete(DeleteBehavior.SetNull);
			builder.HasOne(ar => ar.AssignmentEvent).WithOne(ae => ae.AssignedReward).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
