using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class StatusAssignActDto : ActDto
    {
        public ushort StatusKey { get; set; }
        public bool Override { get; set; } = false;
    }
}
