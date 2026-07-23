using Business.Models.Acts.Abstraction;

namespace Business.Models.Acts
{
    public class RankChangingActDto : ActDto
    {
        public int Steps { get; set; }
        public bool IgnorePostMaxRank { get; set; } = false;
        public bool IsDowngrade { get; set; } = false;
    }
}
