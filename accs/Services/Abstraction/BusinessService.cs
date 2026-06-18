using accs.Logging;
using accs.Models;

namespace accs.Services.Abstraction
{
    public abstract class BusinessService
    {
        protected readonly ILogger _logger;

        public Unit? Actor { get; set; }

        public BusinessService(ILogger logger)
        {
            _logger = logger;

            _logger.LogTrace(EventIds.Processing, "Service called");
        }
    }
}
