using accs.Models.States.Abstraction;

namespace accs.Models.States.Statuses
{
    public class WithoutStatus : Status
    {
        public override int GetIndex()
        {
            return 0;
        }
    }
}
