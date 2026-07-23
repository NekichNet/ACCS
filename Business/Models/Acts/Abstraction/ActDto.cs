namespace Business.Models.Acts.Abstraction
{
    public class ActDto
    {
        public int? DocId { get; set; } = null;
        public HashSet<int> UnitIds { get; set; } = new HashSet<int>();
    }
}
