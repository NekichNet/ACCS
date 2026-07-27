using Business.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace Business.Models.States.Statuses
{
    [Table("Reprimands")]
    public class Reprimand : Status
    {
		[NotMapped] public override int Summand { get; } = -1;

		public override string GetText()
        {
			return "Объявлен выговор";
        }

        public override string? GetHexColor()
        {
            return "#FF0000";
        }

        public override int GetIndex()
        {
            return -1;
        }
    }
}
