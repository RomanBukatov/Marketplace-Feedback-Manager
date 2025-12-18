using Marketplace.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.API.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(ApplicationDbContext context, ILogger<FeedbackService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddLogAsync(FeedbackLog log)
        {
            try
            {
                // Заполняем время, если не указано
                if (log.ProcessedAt == default)
                    log.ProcessedAt = DateTime.UtcNow;

                await _context.FeedbackLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении лога в БД");
                // Мы не выбрасываем ошибку дальше, чтобы не остановить работу бота из-за сбоя статистики
            }
        }

        public async Task<bool> IsProcessedAsync(string reviewId)
        {
            // Проверяем, есть ли в базе запись с таким ReviewId, где мы успешно ответили
            return await _context.FeedbackLogs.AnyAsync(x => x.ReviewId == reviewId && x.IsAutoReplied);
        }
    }
}