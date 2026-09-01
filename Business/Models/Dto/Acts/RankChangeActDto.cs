using Business.Models.Dto.Acts.Abstraction;

namespace Business.Models.Dto.Acts
{
    public class RankChangeActDto : ActDto
    {
        public int Steps { get; set; } = 1;
        public bool IgnorePostMaxRank { get; set; } = false;
        public bool IsDowngrade { get; set; } = false;
    }
}
