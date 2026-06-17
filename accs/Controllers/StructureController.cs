using accs.Services;
using Microsoft.AspNetCore.Mvc;

namespace accs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StructureController : ControllerBase
    {
        private readonly StructureService _structureService;
        private readonly ILogger<StructureController> _logger;

        public StructureController(StructureService structureService, ILogger<StructureController> logger)
        {
            _logger = logger;
            _structureService = structureService;
        }


        [HttpGet]
        public async Task<IActionResult> GetStructure()
        {
            try
            {
                var actionResult = await _structureService.GetStructureAsync();
                if (!actionResult.IsSuccess)
                {
                    return BadRequest(new { error = actionResult.Message });
                }
                return Ok(actionResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetStructure: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
