using accs.Database;
using Microsoft.AspNetCore.Mvc;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StructureController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitController> _logger;

        public StructureController(AppDbContext dbContext, ILogger<UnitController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> GetStructure()
        {
            return await Task.FromResult(Ok());
        }
    }
}
