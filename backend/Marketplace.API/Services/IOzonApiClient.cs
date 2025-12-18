namespace Marketplace.API.Services
{
    public interface IOzonApiClient
    {
        Task CheckForNewReviewsAsync(CancellationToken cancellationToken);
    }
}