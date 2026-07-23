using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class RewardingActDto : ActDto
    {
        HashSet<int> RewardIds { get; set; } = new HashSet<int>();
    }
}
