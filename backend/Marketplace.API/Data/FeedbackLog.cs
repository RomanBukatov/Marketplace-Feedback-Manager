using System;
using System.ComponentModel.DataAnnotations;

namespace Marketplace.API.Data
{
    public class FeedbackLog
    {
        [Key]
        public int Id { get; set; }

        public string Marketplace { get; set; } = string.Empty; // "WB" или "Ozon"
        public string ShopId { get; set; } = string.Empty;      // ClientId или название кабинета
        public string ReviewId { get; set; } = string.Empty;
        public string ReviewText { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string GeneratedResponse { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public bool IsAutoReplied { get; set; } // true - ответил бот, false - пропущен/ошибка
    }
}