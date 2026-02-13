using oamswlatifose.Server.Repository.TokenManagement.Interfaces;

namespace oamswlatifose.Server.BackgroundServices
{
    /// <summary>
    /// Background service for cleaning up expired and revoked tokens.
    /// Runs periodically to maintain database performance.
    /// </summary>
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Run daily

        public TokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokens();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during token cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Token Cleanup Service stopped");
        }

        private async Task CleanupExpiredTokens()
        {
            using var scope = _serviceProvider.CreateScope();
            var tokenRepository = scope.ServiceProvider.GetRequiredService<IJwtTokenManagementCommandRepository>();

            var retentionThreshold = DateTime.UtcNow.AddDays(-30); // Keep last 30 days
            var deletedCount = await tokenRepository.CleanupExpiredTokensAsync(retentionThreshold);

            _logger.LogInformation("Cleaned up {DeletedCount} expired tokens older than {RetentionThreshold}",
                deletedCount, retentionThreshold);
        }
    }
}
