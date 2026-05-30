using accs.Models.States.Abstraction;

namespace accs.Models.States.Statuses
{
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
