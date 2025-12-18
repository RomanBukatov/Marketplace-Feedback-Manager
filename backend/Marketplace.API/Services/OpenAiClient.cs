using Microsoft.Extensions.Options;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using Marketplace.API.Configuration;

namespace Marketplace.API.Services
{
    public class OpenAiClient : IOpenAiClient
    {
        private readonly IOpenAIService _openAiService;
        private readonly ILogger<OpenAiClient> _logger;
        private readonly IOptionsSnapshot<WorkerSettings> _settings;

        public OpenAiClient(IOpenAIService openAiService, ILogger<OpenAiClient> logger, IOptionsSnapshot<WorkerSettings> settings)
        {
            _openAiService = openAiService;
            _logger = logger;
            _settings = settings;
        }

        public async Task<string?> GetResponseForFeedback(string feedbackText, string userName, string marketplaceName, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрашиваем ответ от OpenAI...");

            // Берем промпт из настроек
            var dynamicSystemPrompt = _settings.Value.SystemPrompt + $" Ты работаешь на маркетплейсе {marketplaceName}. Никогда не упоминай названия других маркетплейсов.";

            // Если есть имя, добавляем инструкцию
            if (!string.IsNullOrWhiteSpace(userName))
            {
                dynamicSystemPrompt += $" Обратись к клиенту по имени: {userName}.";
            }

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
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

                // Добавляем подпись, если она есть
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