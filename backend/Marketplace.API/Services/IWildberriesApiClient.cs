namespace Marketplace.API.Services
{
    public interface IWildberriesApiClient
    {
        Task CheckForNewReviewsAsync(CancellationToken cancellationToken);
    }
}