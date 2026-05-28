using Microsoft.AspNetCore.Mvc;

namespace accs.Controllers
{
    [Route ("api/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetUnits(
            [FromQuery] int post,
			[FromQuery] int subdivision,
			[FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUnit()
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnit(
            [FromRoute] ulong id)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUnit(
            [FromRoute] ulong discordId)
        {
            return await Task.FromResult(Ok());
        }
    }
}
