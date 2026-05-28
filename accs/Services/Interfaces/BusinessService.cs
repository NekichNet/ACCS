using accs.Models;

namespace accs.Services.Interfaces
{
    public abstract class BusinessService
    {
        public Unit? Actor { get; }
        public DateTime Start { get; }

        public BusinessService(Unit? actor = null)
        {
            Actor = actor;
            Start = DateTime.UtcNow;
        }
    }
}
