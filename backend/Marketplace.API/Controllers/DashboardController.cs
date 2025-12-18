using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Marketplace.API.Data;

namespace Marketplace.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.UtcNow.Date;
            
            // Получаем то, что есть в базе
            var dbStats = await _context.FeedbackLogs
                .Where(x => x.ProcessedAt >= today)
                .GroupBy(x => x.Marketplace)
                .Select(g => new { Marketplace = g.Key, Count = g.Count() })
                .ToListAsync();

            // Формируем жесткий список, чтобы всегда были оба магазина
            var result = new List<object>
            {
                new
                {
                    Marketplace = "Wildberries",
                    Count = dbStats.FirstOrDefault(s => s.Marketplace == "Wildberries")?.Count ?? 0
                },
                new
                {
                    Marketplace = "Ozon",
                    Count = dbStats.FirstOrDefault(s => s.Marketplace == "Ozon")?.Count ?? 0
                }
            };

            return Ok(result);
        }

        // GET: api/dashboard/logs
        // Возвращает последние 50 логов ответов
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 50, [FromQuery] string? marketplace = null)
        {
            var query = _context.FeedbackLogs.AsQueryable();

            // Если фильтр выбран - применяем его
            if (!string.IsNullOrWhiteSpace(marketplace))
            {
                query = query.Where(x => x.Marketplace == marketplace);
            }

            var logs = await query
                .OrderByDescending(x => x.ProcessedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(logs);
        }

        // GET: api/dashboard/analytics
        // Статистика за последние 7 дней по дням
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
            
            // Грузим сырые данные за неделю (SQLite не любит сложные группировки по датам, проще сделать в памяти)
            var rawData = await _context.FeedbackLogs
                .Where(x => x.ProcessedAt >= sevenDaysAgo)
                .Select(x => new { x.Marketplace, x.ProcessedAt })
                .ToListAsync();

            // Группируем в памяти
            var result = rawData
                .GroupBy(x => x.ProcessedAt.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("dd.MM"),
                    Wildberries = g.Count(x => x.Marketplace == "Wildberries"),
                    Ozon = g.Count(x => x.Marketplace == "Ozon")
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(result);
        }
    }
}