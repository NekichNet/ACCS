using Business.Models.Dto.Acts.Abstraction;

namespace Business.Models.Dto.Acts
{
    public class RewardAssignActDto : ActDto
    {
        public HashSet<int> RewardIds { get; set; } = new HashSet<int>();
    }
}
