using Microsoft.Extensions.Options;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Managers; // Добавлено
using OpenAI; // Добавлено
using Marketplace.API.Configuration;

namespace Marketplace.API.Services
{
    public class OpenAiClient : IOpenAiClient
    {
        private readonly ILogger<OpenAiClient> _logger;
        private readonly IOptionsSnapshot<ApiKeys> _apiKeys; // Берем ключи динамически
        private readonly IOptionsSnapshot<WorkerSettings> _settings;

        public OpenAiClient(
            ILogger<OpenAiClient> logger, 
            IOptionsSnapshot<WorkerSettings> settings,
            IOptionsSnapshot<ApiKeys> apiKeys) // Внедряем ключи
        {
            _logger = logger;
            _settings = settings;
            _apiKeys = apiKeys;
        }

        public async Task<string?> GetResponseForFeedback(string feedbackText, string userName, string marketplaceName, CancellationToken cancellationToken)
        {
            var apiKey = _apiKeys.Value.OpenAI;
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Ключ OpenAI не настроен!");
                return null;
            }

            // Создаем сервис "на лету" с актуальным ключом
            var openAiService = new OpenAIService(new OpenAiOptions() { ApiKey = apiKey });

            _logger.LogInformation("Запрашиваем ответ от OpenAI...");

            // Берем промпт из настроек
            var dynamicSystemPrompt = _settings.Value.SystemPrompt + $" Ты работаешь на маркетплейсе {marketplaceName}. Никогда не упоминай названия других маркетплейсов.";

            if (!string.IsNullOrWhiteSpace(userName))
            {
                dynamicSystemPrompt += $" Обратись к клиенту по имени: {userName}.";
            }

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages =
                [
                    ChatMessage.FromSystem(dynamicSystemPrompt),
                    ChatMessage.FromUser(feedbackText)
                ],
                Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo,
                MaxTokens = 250
            }, cancellationToken: cancellationToken);

            if (completionResult.Successful && completionResult.Choices.Any())
            {
                var responseText = completionResult.Choices.First().Message.Content;
                _logger.LogInformation("Ответ от OpenAI получен.");

                var signature = _settings.Value.Signature;
                if (!string.IsNullOrWhiteSpace(signature))
                {
                    responseText += $"\n\n{signature}";
                }

                return responseText;
            }

            _logger.LogError("Ошибка при получении ответа от OpenAI: {Error}", completionResult.Error?.Message);
            return null;
        }
    }
}