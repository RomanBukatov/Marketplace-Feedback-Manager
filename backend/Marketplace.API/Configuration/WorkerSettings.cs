namespace Marketplace.API.Configuration
{
    public class WorkerSettings
    {
        public int CheckIntervalSeconds { get; set; }

        // Новые настройки
        public int MinRating { get; set; } = 4; // По умолчанию отвечаем только на 4 и 5
        public string SystemPrompt { get; set; } = "Ты — вежливый менеджер поддержки. Твоя задача — поблагодарить клиента за отзыв и пригласить за новыми покупками.";
        public string Signature { get; set; } = ""; // Подпись в конце ответа
    }
}