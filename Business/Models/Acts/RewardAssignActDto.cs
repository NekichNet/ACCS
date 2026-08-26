using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class RewardAssignActDto : ActDto
    {
        public HashSet<int> RewardIds { get; set; } = new HashSet<int>();
    }
}
