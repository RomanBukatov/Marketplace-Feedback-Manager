namespace Marketplace.API.Configuration
{
    public class ApiKeys
    {
        public List<WbAccountCredentials> WildberriesAccounts { get; set; } = [];
        public string OpenAI { get; set; } = string.Empty;
        
        // Список аккаунтов Ozon
        public List<OzonAccountCredentials> OzonAccounts { get; set; } = [];
    }

    public class OzonAccountCredentials
    {
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class WbAccountCredentials
    {
        public string Token { get; set; } = string.Empty;
    }
}