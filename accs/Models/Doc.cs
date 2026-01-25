using accs.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace accs.Models
{
	[EntityTypeConfiguration(typeof(DocConfiguration))]
	public class Doc
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string FilePath { get; set; }
		public virtual DocType? DocType { get; set; }
		public virtual Unit Author { get; set; }
		public virtual List<Unit> Units { get; set; } = new List<Unit>(); // Люди, связанные с этим документом
	}
}
