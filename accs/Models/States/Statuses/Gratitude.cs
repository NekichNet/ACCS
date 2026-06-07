using accs.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.States.Statuses
{
	[Table("Gratitudes")]
    public class Gratitude : Status
    {
		public override string GetText()
		{
			return "Благодарность";
		}

		public override string? GetHexColor()
		{
			return "#00FFAA";
		}

        public override int GetIndex()
        {
            return 1;
        }
    }
}
