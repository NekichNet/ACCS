namespace accs.Models
{
	public class DocType
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public string? TemplatePath { get; set; } // Указывает на образец для составления документа
		public List<Doc> Docs { get; set; } = new List<Doc>();
	}
}
