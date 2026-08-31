using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class RankChangeActDto : ActDto
    {
        public int Steps { get; set; } = 1;
        public bool IgnorePostMaxRank { get; set; } = false;
        public bool IsDowngrade { get; set; } = false;
    }
}
