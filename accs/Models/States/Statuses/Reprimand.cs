using accs.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.States.Statuses
{
    [Table("Reprimands")]
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
