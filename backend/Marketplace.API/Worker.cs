using Microsoft.Extensions.Options;
using Marketplace.API.Configuration;
using Marketplace.API.Services;

namespace Marketplace.API
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkerSettings _workerSettings;
        private readonly WorkerStateService _stateService;

        public Worker(
            ILogger<Worker> logger,
            IServiceProvider serviceProvider,
            IOptions<WorkerSettings> workerSettings,
            WorkerStateService stateService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _workerSettings = workerSettings.Value;
            _stateService = stateService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker запущен в: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                // ПРОВЕРКА РУБИЛЬНИКА
                if (!_stateService.IsRunning)
                {
                    // Если выключено - просто ждем секунду и проверяем снова
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                // СОЗДАЕМ ОБЛАСТЬ ВИДИМОСТИ (SCOPE)
                // Это нужно для работы с Базой Данных внутри Singleton-сервиса
                using (var scope = _serviceProvider.CreateScope())
                {
                    var wbApiClient = scope.ServiceProvider.GetRequiredService<IWildberriesApiClient>();
                    var ozonApiClient = scope.ServiceProvider.GetRequiredService<IOzonApiClient>();

                    // 1. Wildberries
                    try
                    {
                        await wbApiClient.CheckForNewReviewsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Произошла ошибка при проверке WB");
                    }

                    // 2. Ozon
                    try
                    {
                        await ozonApiClient.CheckForNewReviewsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Произошла ошибка при проверке Ozon");
                    }
                }

                _logger.LogInformation("\n==================== ЦИКЛ ОБРАБОТКИ ЗАВЕРШЕН ====================\n");

                var delay = TimeSpan.FromSeconds(_workerSettings.CheckIntervalSeconds);
                _logger.LogInformation("Следующая проверка через {Delay} секунд...", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
