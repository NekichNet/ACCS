using Business.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace Business.Models.States.Statuses
{
	[Table("SevereReprimands")]
    public class SevereReprimand : Status
    {
		[NotMapped] public override int Summand { get; } = -2;

		public override string GetText()
		{
			return "Объявлен строгий выговор";
		}

		public override string? GetHexColor()
		{
			return "#990000";
		}

        public override int GetIndex()
        {
            return -2;
        }
    }
}
