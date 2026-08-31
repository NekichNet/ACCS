using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class StatusAssignActDto : ActDto
    {
        public ushort StatusKey { get; set; }
        public bool Ovewrite { get; set; } = false;
		public DateTime? Start { get; set; } = null;
		public DateTime? End { get; set; } = null;
        public int Days { get; set; } = 7;
	}
}
