using accs.Models.States.Abstraction;
using accs.Models.Statuses.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.Statuses
{
    [Table("AssignedPosts")]
    public class AssignedPost : StateWithDoc
    {
        public int PostId { get; set; }
        [JsonIgnore] public virtual Post Post { get; set; }
    }
}
