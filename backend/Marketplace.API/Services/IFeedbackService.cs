namespace Marketplace.API.Services
{
    using Marketplace.API.Data;

    public interface IFeedbackService
    {
        Task AddLogAsync(FeedbackLog log);
        Task<bool> IsProcessedAsync(string reviewId);
    }
}