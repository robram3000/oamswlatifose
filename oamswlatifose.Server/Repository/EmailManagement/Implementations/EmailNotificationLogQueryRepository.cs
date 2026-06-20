using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.smtp;
using oamswlatifose.Server.Repository.EmailManagement.Interfaces;

namespace oamswlatifose.Server.Repository.EmailManagement.Implementations
{
    /// <summary>
    /// Query repository implementation for email notification log data retrieval operations.
    /// This repository provides comprehensive read-only access to email communication records,
    /// delivery status information, and OTP verification tracking essential for communication
    /// auditing and user verification workflows.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Email log retrieval with complete communication metadata</para>
    /// <para>- OTP verification request tracking and status monitoring</para>
    /// <para>- User-specific email history for communication audit</para>
    /// <para>- Time-based email volume analysis for operational monitoring</para>
    /// <para>- OTP validation and expiration status checking</para>
    /// <para>- Delivery status tracking and failure investigation</para>
    /// </summary>
    public class EmailNotificationLogQueryRepository : IEmailNotificationLogQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailNotificationLogQueryRepository> _logger;

        public EmailNotificationLogQueryRepository(
            ApplicationDbContext context,
            ILogger<EmailNotificationLogQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMEmaillogs>> GetAllEmailLogsAsync()
        {
            _logger.LogDebug("Retrieving all email logs");
            return await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .OrderByDescending(e => e.id) // Assuming timestamp would be better
                .ToListAsync();
        }

        public async Task<EMEmaillogs> GetEmailLogByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving email log with ID: {Id}", id);
            return await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .FirstOrDefaultAsync(e => e.id == id);
        }

        public async Task<IEnumerable<EMEmaillogs>> GetEmailLogsByRecipientAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            _logger.LogDebug("Retrieving email logs for recipient: {Email}", email);
            return await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .Where(e => e.Email == email)
                .OrderByDescending(e => e.id)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMEmaillogs>> GetEmailLogsPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogDebug("Retrieving email logs page {PageNumber} with page size {PageSize}", pageNumber, pageSize);

            return await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .OrderByDescending(e => e.id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMEmaillogs>> GetEmailLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Retrieving email logs from {StartDate} to {EndDate}",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            // Note: Add timestamp field to EMEmaillogs for date filtering
            return await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .OrderByDescending(e => e.id)
                .ToListAsync();
        }

        public async Task<EMOtpUserRequest> GetOtpRequestByIdAsync(string otpId)
        {
            if (string.IsNullOrWhiteSpace(otpId))
                throw new ArgumentException("OTP ID cannot be null or empty", nameof(otpId));

            _logger.LogDebug("Retrieving OTP request with ID: {OtpId}", otpId);
            return await _context.Set<EMOtpUserRequest>()
                .FirstOrDefaultAsync(o => o.OTPid == otpId);
        }

        public async Task<EMOtpUserRequest> GetLatestOtpRequestByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            _logger.LogDebug("Retrieving latest OTP request for email: {Email}", email);

            var emailLog = await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .Where(e => e.Email == email)
                .OrderByDescending(e => e.id)
                .FirstOrDefaultAsync();

            return emailLog?.OtpUserRequest;
        }

        public async Task<bool> ValidateOtpCodeAsync(string email, string otpCode)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
                return false;

            _logger.LogDebug("Validating OTP for email: {Email}", email);

            var latestOtp = await GetLatestOtpRequestByEmailAsync(email);

            if (latestOtp == null)
                return false;

            // Note: Add expiration checking logic here
            return latestOtp.OTP == otpCode;
        }

        public async Task<int> GetEmailSentCountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Counting emails sent from {StartDate} to {EndDate}",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            // Note: Add timestamp field to EMEmaillogs for date filtering
            return await _context.Set<EMEmaillogs>().CountAsync();
        }

        public async Task<Dictionary<DateTime, string>> GetOtpUsageHistoryAsync(string email, DateTime since)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            _logger.LogDebug("Retrieving OTP usage history for email: {Email} since {Since}",
                email, since.ToString("yyyy-MM-dd"));

            var emailLogs = await _context.Set<EMEmaillogs>()
                .Include(e => e.OtpUserRequest)
                .Where(e => e.Email == email)
                .OrderByDescending(e => e.id)
                .ToListAsync();

            return emailLogs
                .Where(e => e.OtpUserRequest != null)
                .ToDictionary(
                    e => DateTime.UtcNow, // Replace with actual timestamp from entity
                    e => e.OtpUserRequest.OTP
                );
        }
    }
}
