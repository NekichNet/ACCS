using Business.Models.Dto.Acts.Abstraction;

namespace Business.Models.Dto.Acts
{
	public class ReturnToActiveActDto : ActDto
	{
		public int RankId { get; set; }
		public HashSet<int> PostIds { get; set; }
	}
}
