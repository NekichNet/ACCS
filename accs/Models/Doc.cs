using accs.Models.Configurations;
using accs.Models.SingleDayEvents.Abstraction;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace accs.Models
{
	public class Doc
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public ulong AuthorId { get; set; }
		[JsonIgnore] public virtual Unit Author { get; set; }
		public int EventId { get; set; }
		[JsonIgnore] public virtual EventWithDoc Event { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
	}
}
