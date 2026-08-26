using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class PostAssignActDto : ActDto
    {
        public HashSet<int> PostIds { get; set; } = new HashSet<int>();
        public bool Overwrite { get; set; } = false;
    }
}
