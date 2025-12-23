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
        private readonly IOptionsSnapshot<ApiKeys> _apiKeys; // Используем Snapshot для свежих данных

        public WildberriesApiClient(
            HttpClient httpClient,
            ILogger<WildberriesApiClient> logger,
            IOptionsSnapshot<ApiKeys> apiKeys, // Заменили на Snapshot
            IOpenAiClient openAiClient,
            IFeedbackService feedbackService,
            IOptionsSnapshot<WorkerSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _openAiClient = openAiClient;
            _feedbackService = feedbackService;
            _settings = settings;
            _apiKeys = apiKeys;

            _httpClient.BaseAddress = new Uri("https://feedbacks-api.wildberries.ru");
        }

        public async Task CheckForNewReviewsAsync(CancellationToken cancellationToken)
        {
            // Проверка наличия аккаунтов
            if (_apiKeys.Value.WildberriesAccounts == null || _apiKeys.Value.WildberriesAccounts.Count == 0)
            {
                _logger.LogWarning("Список аккаунтов Wildberries пуст. Пропускаем.");
                return;
            }

            int accountCounter = 1;
            foreach (var account in _apiKeys.Value.WildberriesAccounts)
            {
                if (string.IsNullOrWhiteSpace(account.Token)) continue;

                _logger.LogInformation("--- Проверяем Wildberries кабинет №{Num} ---", accountCounter++);
                await ProcessAccountAsync(account, cancellationToken);
            }
        }

        private async Task ProcessAccountAsync(WbAccountCredentials account, CancellationToken cancellationToken)
        {
            try
            {
                var requestUrl = "/api/v1/feedbacks?isAnswered=false&take=100&skip=0";
                
                // Используем метод с динамическим токеном
                var response = await SendRequestAsync(HttpMethod.Get, requestUrl, null, account.Token, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ошибка Wildberries API: {StatusCode}", response.StatusCode);
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
                    if (feedback.Valuation != 0 && feedback.Valuation < _settings.Value.MinRating)
                    {
                        _logger.LogInformation("Отзыв WB {Id} пропущен: Рейтинг {Rate} ниже порога {Min}.", feedback.Id, feedback.Valuation, _settings.Value.MinRating);
                        continue;
                    }

                    // 1. Сборка текста
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(feedback.Text)) parts.Add(feedback.Text);
                    if (!string.IsNullOrWhiteSpace(feedback.Pros)) parts.Add($"Достоинства: {feedback.Pros}");
                    if (!string.IsNullOrWhiteSpace(feedback.Cons)) parts.Add($"Недостатки: {feedback.Cons}");

                    var fullText = string.Join("\n", parts);

                    // 2. Пустой отзыв
                    if (string.IsNullOrWhiteSpace(fullText))
                    {
                        _logger.LogWarning("Отзыв ID: {FeedbackId} пустой. Отправляем заглушку.", feedback.Id);
                        await SendResponseAsync(feedback.Id, "Благодарим за высокую оценку!", "Пустой отзыв", feedback.Valuation, account, cancellationToken);
                        continue;
                    }

                    // 3. Генерация AI
                    var responseText = await _openAiClient.GetResponseForFeedback(fullText, feedback.UserName, "Wildberries", cancellationToken);
                    _logger.LogInformation("Для отзыва '{FeedbackText}' сгенерирован ответ: '{ResponseText}'", fullText.Replace("\n", " | "), responseText);

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        _logger.LogWarning("OpenAI не сгенерировал ответ. Пропускаем.");
                        continue;
                    }

                    // 4. Отправка
                    await SendResponseAsync(feedback.Id, responseText, fullText, feedback.Valuation, account, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке кабинета WB.");
            }
        }

        private async Task SendResponseAsync(string feedbackId, string responseText, string originalReviewText, int valuation, WbAccountCredentials account, CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new WbFeedbackPatchRequest(feedbackId, responseText);
                var requestUrl = "/api/v1/feedbacks/answer";

                var response = await SendRequestAsync(HttpMethod.Post, requestUrl, requestBody, account.Token, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ответ на отзыв ID: {FeedbackId} успешно отправлен.", feedbackId);
                    await _feedbackService.AddLogAsync(new FeedbackLog
                    {
                        Marketplace = "Wildberries",
                        // Используем маску токена как ID магазина (или можно добавить Name в конфиг в будущем)
                        ShopId = account.Token.Substring(0, Math.Min(10, account.Token.Length)) + "...", 
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
                    _logger.LogError("Не удалось отправить ответ WB: {StatusCode}. {Error}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отправке ответа WB.");
            }
        }

        // Утилитный метод для запросов с конкретным токеном
        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string url, object? content, string token, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", token);

            if (content != null)
            {
                var json = JsonSerializer.Serialize(content);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return await _httpClient.SendAsync(request, cancellationToken);
        }
    }
}