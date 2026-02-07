using oamswlatifose.Server.Smtp;

namespace oamswlatifose.Server.Repository.Rsmtp
{
    public interface IEmailLogRepository
    {
        Task AddAsync(EmailLog emailLog);
        Task<List<EmailLog>> GetLogsAsync(string email = null, DateTime? fromDate = null, DateTime? toDate = null);
    }

    // Simple in-memory implementation for demonstration
    public class InMemoryEmailLogRepository : IEmailLogRepository
    {
        private readonly List<EmailLog> _logs = new List<EmailLog>();

        public Task AddAsync(EmailLog emailLog)
        {
            _logs.Add(emailLog);
            return Task.CompletedTask;
        }

        public Task<List<EmailLog>> GetLogsAsync(string email = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _logs.AsQueryable();

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(x => x.ToEmail.Contains(email, StringComparison.OrdinalIgnoreCase));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= toDate.Value);
            }

            return Task.FromResult(query.OrderByDescending(x => x.CreatedAt).ToList());
        }
    }
}
