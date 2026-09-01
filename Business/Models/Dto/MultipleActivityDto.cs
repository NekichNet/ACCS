namespace Business.Models.Dto
{
	public class MultipleActivityDto
	{
		public HashSet<ulong> UnitIds { get; set; } = new HashSet<ulong>();
		public ulong EndDateUnix { get; set; }
	}
}
