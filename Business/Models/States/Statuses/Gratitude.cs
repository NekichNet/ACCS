using Business.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace Business.Models.States.Statuses
{
	[Table("Gratitudes")]
    public class Gratitude : Status
    {
		public override string GetText()
		{
			return $"Объявлена благодарность";
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
