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
        public bool Display { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
