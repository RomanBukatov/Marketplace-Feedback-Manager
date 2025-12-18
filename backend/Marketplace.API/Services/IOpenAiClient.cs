namespace Marketplace.API.Services
{
    public interface IOpenAiClient
    {
        Task<string?> GetResponseForFeedback(string feedbackText, string userName, string marketplaceName, CancellationToken cancellationToken);
    }
}