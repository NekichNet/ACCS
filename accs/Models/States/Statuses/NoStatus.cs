using accs.Models.States.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.States.Statuses
{
    [Table("NoStatuses")]
    public class NoStatus : Status
    {
        public override int GetIndex()
        {
            return 0;
        }
    }
}
