using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace accs.Controllers
{
    [Route ("api/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetUnit(
            [FromQuery] int post,
            [FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost]
        public async Task<IActionResult> CreateUnit(
            [FromQuery] int post,
            [FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnitById(
            [FromRoute] ulong id)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUnitById(
            [FromRoute] ulong discordId,
            [FromQuery] int post,
            [FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }
    }
}
