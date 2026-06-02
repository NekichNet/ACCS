using accs.Models.Statuses.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.States
{
    [Table("Retirements")]
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
