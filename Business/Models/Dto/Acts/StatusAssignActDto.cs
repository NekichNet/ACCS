using Business.Models.Dto.Acts.Abstraction;

namespace Business.Models.Dto.Acts
{
    public class StatusAssignActDto : ActDto
    {
        public ushort StatusKey { get; set; }
        public bool Ovewrite { get; set; } = false;
		public DateTime? End { get; set; } = null;
        public int Days { get; set; } = 7;
	}
}
