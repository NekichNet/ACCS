using accs.Models.States.Abstraction;

namespace accs.Models.States.Statuses
{
    public class Reprimand : Status
    {
        public override string GetText()
        {
			return "Выговор";
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
