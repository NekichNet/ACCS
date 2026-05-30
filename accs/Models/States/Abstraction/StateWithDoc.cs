using accs.Models.Statuses.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace accs.Models.States.Abstraction
{
	[Table("StatesWithDoc")]
	public abstract class StateWithDoc : UnitState
	{
		public int DocId { get; set; }
		[JsonIgnore] public virtual Doc Doc { get; set; }
	}
}
