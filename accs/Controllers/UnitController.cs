using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace accs.Controllers
{
    [Route ("api/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        [HttpGet("unit")]
        public async Task<IActionResult> GetUnit(
            [FromQuery] int post,
            [FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPost("unit")]
        public async Task<IActionResult> CreateUnit(
            [FromQuery] int post,
            [FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }

        [HttpGet("unit/{id}")]
        public async Task<IActionResult> GetUnitById(
            [FromRoute] int id)
        {
            return await Task.FromResult(Ok());
        }

        [HttpPatch("unit/{id}")]
        public async Task<IActionResult> UpdateUnitById(
            [FromRoute] int id,
            [FromQuery] int post,
            [FromQuery] int rank,
            [FromQuery] int reward)
        {
            return await Task.FromResult(Ok());
        }
    }
}
