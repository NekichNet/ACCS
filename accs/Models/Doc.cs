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
		public DocType DocType { get; set; }
		public Unit Author { get; set; }
		public List<Unit> Units { get; set; } = new List<Unit>(); // Люди, связанные с этим документов
		public List<string> RoleIds { get; set; } = new List<string>(); // Список связанных с этим документом ролей из ID через ";"
	}
}
