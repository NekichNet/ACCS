using Business.Models.Dto.Acts.Abstraction;

namespace Business.Models.Dto.Acts
{
    public class PostAssignActDto : ActDto
    {
        public HashSet<int> PostIds { get; set; } = new HashSet<int>();
        public bool Overwrite { get; set; } = false;
    }
}
