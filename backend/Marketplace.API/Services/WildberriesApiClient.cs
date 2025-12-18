using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Marketplace.API.Configuration;
using Marketplace.API.DTOs;
using Marketplace.API.Data;

namespace Marketplace.API.Services
{
    public class WildberriesApiClient : IWildberriesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WildberriesApiClient> _logger;
        private readonly IOpenAiClient _openAiClient;
        private readonly IFeedbackService _feedbackService;
        private readonly IOptionsSnapshot<WorkerSettings> _settings;

        public WildberriesApiClient(
            HttpClient httpClient,
            ILogger<WildberriesApiClient> logger,
            IOptions<ApiKeys> apiKeys,
            IOpenAiClient openAiClient,
            IFeedbackService feedbackService,
            IOptionsSnapshot<WorkerSettings> settings) // Добавили зависимость
        {
            _httpClient = httpClient;
            _logger = logger;
            _openAiClient = openAiClient;
            _feedbackService = feedbackService;
            _settings = settings;
            var apiKeysConfig = apiKeys.Value;

            _httpClient.BaseAddress = new Uri("https://feedbacks-api.wildberries.ru");
            _httpClient.DefaultRequestHeaders.Add("Authorization", apiKeysConfig.Wildberries);
        }

        public async Task CheckForNewReviewsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Проверяем наличие новых отзывов на Wildberries...");

            var requestUrl = "/api/v1/feedbacks?isAnswered=false&take=100&skip=0";
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ошибка при получении отзывов от WB API: {StatusCode}", response.StatusCode);
                return;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var feedbackResponse = await JsonSerializer.DeserializeAsync<WbFeedbackResponse>(responseStream, cancellationToken: cancellationToken);

            if (feedbackResponse?.Data.Feedbacks.Count == 0)
            {
                _logger.LogInformation("Новых отзывов нет.");
                return;
            }

            _logger.LogInformation("Найдено {Count} новых отзывов.", feedbackResponse.Data.Feedbacks.Count);

            foreach (var feedback in feedbackResponse.Data.Feedbacks)
            {
                // 0. ФИЛЬТР ПО РЕЙТИНГУ
                // Если рейтинг отзыва меньше минимального - пропускаем
                if (feedback.Valuation != 0 && feedback.Valuation < _settings.Value.MinRating)
                {
                    _logger.LogInformation("Отзыв WB {Id} пропущен: Рейтинг {Rate} ниже порога {Min}.", feedback.Id, feedback.Valuation, _settings.Value.MinRating);
                    continue;
                }

                // 1. Собираем полный текст из комментария, плюсов и минусов
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(feedback.Text)) parts.Add(feedback.Text);
                if (!string.IsNullOrWhiteSpace(feedback.Pros)) parts.Add($"Достоинства: {feedback.Pros}");
                if (!string.IsNullOrWhiteSpace(feedback.Cons)) parts.Add($"Недостатки: {feedback.Cons}");

                var fullText = string.Join("\n", parts);

                // 2. Если отзыв СОВСЕМ пустой (нет ни текста, ни плюсов, ни минусов)
                if (string.IsNullOrWhiteSpace(fullText))
                {
                    _logger.LogWarning("Отзыв ID: {FeedbackId} вообще без текста. Отправляем стандартный ответ, чтобы убрать его из очереди.", feedback.Id);
                    // ОБЯЗАТЕЛЬНО отвечаем, иначе WB будет присылать этот отзыв вечно, и очередь встанет
                    await SendResponseAsync(feedback.Id, "Благодарим за высокую оценку!", "Пустой отзыв", feedback.Valuation, cancellationToken);
                    continue;
                }

                // 3. Если текст есть — генерируем ответ через OpenAI
                var responseText = await _openAiClient.GetResponseForFeedback(fullText, feedback.UserName, "Wildberries", cancellationToken);
                _logger.LogInformation("Для отзыва '{FeedbackText}' сгенерирован ответ: '{ResponseText}'", fullText.Replace("\n", " | "), responseText);

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("OpenAI не сгенерировал ответ для отзыва ID: {FeedbackId}. Пропускаем.", feedback.Id);
                    continue;
                }

                await SendResponseAsync(feedback.Id, responseText, fullText, feedback.Valuation, cancellationToken);
            }

            _logger.LogInformation("Проверка и обработка отзывов завершена.");
        }

        private async Task SendResponseAsync(string feedbackId, string responseText, string originalReviewText, int valuation, CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new WbFeedbackPatchRequest(feedbackId, responseText);
                var jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // ФИНАЛЬНЫЙ ПРАВИЛЬНЫЙ URL И МЕТОД
                var requestUrl = "/api/v1/feedbacks/answer";

                _logger.LogInformation("Отправляем ответ для отзыва ID: {FeedbackId}", feedbackId);

                var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ответ на отзыв ID: {FeedbackId} успешно отправлен.", feedbackId);

                    // СОХРАНЯЕМ В БД
                    await _feedbackService.AddLogAsync(new FeedbackLog
                    {
                        Marketplace = "Wildberries",
                        ShopId = "Default", // Пока хардкод, потом возьмем из конфига
                        ReviewId = feedbackId,
                        ReviewText = originalReviewText,
                        GeneratedResponse = responseText,
                        IsAutoReplied = true,
                        Rating = valuation
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Не удалось отправить ответ на отзыв ID: {FeedbackId}. Статус: {StatusCode}. Ошибка: {Error}",
                        feedbackId, response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отправке ответа на отзыв ID: {FeedbackId}", feedbackId);
            }
        }
    }
}