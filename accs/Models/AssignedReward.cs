using accs.Models.Configurations;
using accs.Models.SingleDayEvents;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(AssignedRewardConfiguration))]
	public class AssignedReward
    {
        public int RewardId { get; set; }
		public bool Display { get; set; }
		[JsonIgnore] public virtual Reward Reward { get; set; }
        public ulong UnitId { get; set; }
        [JsonIgnore] public virtual Unit Unit { get; set; }
        public int AssignmentEventId { get; set; }
        [JsonIgnore] public virtual RewardAssignmentEvent AssignmentEvent { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
