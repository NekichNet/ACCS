using accs.Models.States.Abstraction;
using accs.Models.Statuses.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.Statuses
{
    [Table("AssignedRanks")]
    public class AssignedRank : UnitState
    {
        public int RankId { get; set; }
        [JsonIgnore] public virtual Rank Rank { get; set; }
    }
}
