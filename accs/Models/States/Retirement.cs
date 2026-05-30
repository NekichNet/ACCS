using accs.Models.Statuses.Abstraction;

namespace accs.Models.States
{
    public class Retirement : UnitState
    {
        public override string GetText()
        {
            return "Отставка";
        }

        public override string? GetHexColor()
        {
            return "#333333";
        }
    }
}
