using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Marketplace.API.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        // Сохраняем новые настройки (MERGE, чтобы не стереть пароль)
        [HttpPost]
        public async Task<IActionResult> UpdateSettings([FromBody] JsonElement newSettings)
        {
            var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            
            // 1. Читаем текущий файл целиком (чтобы сохранить Auth и Logging)
            var currentJsonString = await System.IO.File.ReadAllTextAsync(configPath);
            var currentNode = JsonNode.Parse(currentJsonString)!.AsObject();

            // 2. Парсим то, что пришло с фронта
            var incomingNode = JsonNode.Parse(newSettings.GetRawText())!.AsObject();

            // 3. Точечно обновляем секции
            if (incomingNode.ContainsKey("ApiKeys"))
            {
                currentNode["ApiKeys"] = incomingNode["ApiKeys"]!.DeepClone();
            }

            if (incomingNode.ContainsKey("WorkerSettings"))
            {
                currentNode["WorkerSettings"] = incomingNode["WorkerSettings"]!.DeepClone();
            }

            // Секция "Auth" останется нетронутой, так как мы её не меняли в currentNode

            // 4. Сохраняем обратно
            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(configPath, currentNode.ToJsonString(options));

            return Ok(new { message = "Settings updated successfully." });
        }
    }
}