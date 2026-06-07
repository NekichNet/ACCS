using accs.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.States.Statuses
{
	[Table("SevereReprimands")]
    public class SevereReprimand : Status
    {
		public override string GetText()
		{
			return "Строгий выговор";
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
