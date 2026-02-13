using oamswlatifose.Server.Repository.SessionManagement.Interfaces;

namespace oamswlatifose.Server.BackgroundServices
{
    /// <summary>
    /// Background service for cleaning up expired sessions.
    /// Runs periodically to maintain database performance.
    /// </summary>
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6); 

        public SessionCleanupService(
            IServiceProvider serviceProvider,
            ILogger<SessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessions();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Session Cleanup Service stopped");
        }

        private async Task CleanupExpiredSessions()
        {
            using var scope = _serviceProvider.CreateScope();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionManagementCommandRepository>();

            var retentionThreshold = DateTime.UtcNow.AddDays(-7); // Keep last 7 days
            var deletedCount = await sessionRepository.CleanupExpiredSessionsAsync(retentionThreshold);

            _logger.LogInformation("Cleaned up {DeletedCount} expired sessions older than {RetentionThreshold}",
                deletedCount, retentionThreshold);
        }
    }
}
