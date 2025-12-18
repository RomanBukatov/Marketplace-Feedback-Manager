using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Marketplace.API.Services;

namespace Marketplace.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RunController : ControllerBase
    {
        private readonly WorkerStateService _stateService;

        public RunController(WorkerStateService stateService)
        {
            _stateService = stateService;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { isRunning = _stateService.IsRunning });
        }

        [HttpPost("start")]
        public IActionResult Start()
        {
            _stateService.Start();
            return Ok(new { message = "Bot started", isRunning = true });
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            _stateService.Stop();
            return Ok(new { message = "Bot stopped", isRunning = false });
        }
    }
}