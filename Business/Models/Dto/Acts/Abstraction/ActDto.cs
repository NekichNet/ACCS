namespace Business.Models.Dto.Acts.Abstraction
{
    public class ActDto
    {
        public int? DocId { get; set; } = null;
        public HashSet<ulong> UnitIds { get; set; } = new HashSet<ulong>();
    }
}
