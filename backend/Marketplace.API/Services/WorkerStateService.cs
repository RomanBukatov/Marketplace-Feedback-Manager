namespace Marketplace.API.Services
{
    public class WorkerStateService
    {
        // По умолчанию false - чтобы при запуске сервера бот МОЛЧАЛ, пока мы не нажмем кнопку
        public bool IsRunning { get; private set; } = false;

        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;
    }
}