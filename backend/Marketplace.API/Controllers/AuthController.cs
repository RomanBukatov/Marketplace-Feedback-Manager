using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using System.IO;

namespace Marketplace.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AuthController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public class LoginRequest
        {
            public string Password { get; set; } = string.Empty;
        }

        public class ChangePasswordRequest
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // ЧИТАЕМ ПАРОЛЬ НАПРЯМУЮ ИЗ ФАЙЛА (чтобы он был свежим)
            string adminPassword = "";
            try 
            {
                var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(configPath);
                    var jsonObj = System.Text.Json.Nodes.JsonNode.Parse(json);
                    adminPassword = jsonObj?["Auth"]?["AdminPassword"]?.ToString() ?? "";
                }
            }
            catch 
            {
                // Если не удалось прочитать файл, пробуем взять из памяти (резерв)
                adminPassword = _configuration["Auth:AdminPassword"] ?? "";
            }

            if (string.IsNullOrEmpty(adminPassword))
            {
                return Unauthorized(new { message = "Ошибка конфигурации: Пароль не задан." });
            }

            if (request.Password != adminPassword)
            {
                return Unauthorized(new { message = "Неверный пароль" });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            return Ok(new { message = "Успешный вход" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Вышли из системы" });
        }
        
        [HttpGet("check")]
        public IActionResult Check()
        {
            if (User.Identity?.IsAuthenticated == true)
                return Ok(new { isAuthenticated = true });

            return Unauthorized();
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            string currentRealPassword = "";

            // 1. Читаем АКТУАЛЬНЫЙ пароль с диска
            try 
            {
                if (System.IO.File.Exists(configPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(configPath);
                    var jsonObj = System.Text.Json.Nodes.JsonNode.Parse(json);
                    currentRealPassword = jsonObj?["Auth"]?["AdminPassword"]?.ToString() ?? "";
                }
            }
            catch 
            {
                currentRealPassword = _configuration["Auth:AdminPassword"] ?? "";
            }

            // 2. Проверяем старый пароль
            if (request.OldPassword != currentRealPassword)
            {
                return BadRequest(new { message = "Старый пароль введен неверно" });
            }

            // 3. Обновляем файл
            var fullJson = await System.IO.File.ReadAllTextAsync(configPath);
            var node = System.Text.Json.Nodes.JsonNode.Parse(fullJson);

            if (node?["Auth"] is System.Text.Json.Nodes.JsonObject authSection)
            {
                authSection["AdminPassword"] = request.NewPassword;
            }
            else
            {
                 // Если секции нет - создаем
                 if (node != null)
                 {
                    node["Auth"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["AdminPassword"] = request.NewPassword
                    };
                 }
            }

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(configPath, node?.ToJsonString(options));

            return Ok(new { message = "Пароль успешно изменен. Пожалуйста, перезайдите." });
        }
    }
}