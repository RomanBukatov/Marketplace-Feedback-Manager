using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Marketplace.API.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public SettingsController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        // GET: api/settings
        // Читаем текущие настройки (скрывая реальные ключи для безопасности, если надо, но пока отдадим как есть)
        [HttpGet]
        public IActionResult GetSettings()
        {
            // Читаем файл напрямую, чтобы получить актуальное состояние
            var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            if (!System.IO.File.Exists(configPath)) return NotFound();

            var json = System.IO.File.ReadAllText(configPath);
            return Content(json, "application/json");
        }

        // POST: api/settings
        // Сохраняем новые настройки
        [HttpPost]
        public async Task<IActionResult> UpdateSettings([FromBody] JsonElement newSettings)
        {
            var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            
            // Красиво форматируем JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(newSettings, options);

            await System.IO.File.WriteAllTextAsync(configPath, jsonString);

            return Ok(new { message = "Settings updated. Restart required to apply changes." });
        }
    }
}