namespace accs.Models
{
    public class TemporaryType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Temporary> Temporaries { get; set; }
    }
}
