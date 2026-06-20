using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Model.user;
using oamswlatifose.Server.Repository.AttendanceManagement.Interfaces;

namespace oamswlatifose.Server.Repository.AttendanceManagement.Implementation
{
    /// <summary>
    /// Query repository implementation for attendance data retrieval operations with advanced filtering,
    /// date-range queries, and employee-specific attendance tracking. This repository provides comprehensive
    /// read-only access to attendance records optimized for performance through efficient query patterns
    /// and proper indexing strategies.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Date-based attendance retrieval with range filtering</para>
    /// <para>- Employee attendance history with chronological ordering</para>
    /// <para>- Attendance status and shift-based filtering</para>
    /// <para>- Hours worked and overtime calculations</para>
    /// <para>- Department-wide attendance summaries</para>
    /// <para>- Late arrival and absence tracking</para>
    /// </summary>
    public class AttendanceTrackingQueryRepository : IAttendanceTrackingQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceTrackingQueryRepository> _logger;

        public AttendanceTrackingQueryRepository(
            ApplicationDbContext context,
            ILogger<AttendanceTrackingQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMAttendance>> GetAllAttendanceRecordsAsync()
        {
            _logger.LogDebug("Retrieving all attendance records");
            return await _context.EMAttendance
                .Include(a => a.Employee)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee.LastName)
                .ToListAsync();
        }

        public async Task<EMAttendance> GetAttendanceByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving attendance record with ID: {Id}", id);
            return await _context.EMAttendance
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<EMAttendance>> GetAttendanceByEmployeeIdAsync(int employeeId)
        {
            _logger.LogDebug("Retrieving attendance records for employee ID: {EmployeeId}", employeeId);
            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAttendance>> GetEmployeeAttendancePaginatedAsync(int employeeId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogDebug("Retrieving attendance for employee {EmployeeId} - Page {PageNumber}, Size {PageSize}",
                employeeId, pageNumber, pageSize);

            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AttendanceDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAttendance>> GetAttendanceByDateAsync(DateTime date)
        {
            _logger.LogDebug("Retrieving attendance records for date: {Date}", date.ToString("yyyy-MM-dd"));
            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate.Date == date.Date)
                .OrderBy(a => a.Employee.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAttendance>> GetAttendanceByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Retrieving attendance records from {StartDate} to {EndDate}",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate >= startDate.Date && a.AttendanceDate <= endDate.Date)
                .OrderBy(a => a.AttendanceDate)
                .ThenBy(a => a.Employee.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAttendance>> GetEmployeeAttendanceByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Retrieving attendance for employee {EmployeeId} from {StartDate} to {EndDate}",
                employeeId, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId &&
                           a.AttendanceDate >= startDate.Date &&
                           a.AttendanceDate <= endDate.Date)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAttendance>> GetAttendanceByStatusAsync(string status)
        {
            _logger.LogDebug("Retrieving attendance records with status: {Status}", status);
            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAttendance>> GetAttendanceByShiftAsync(string shift)
        {
            _logger.LogDebug("Retrieving attendance records for shift: {Shift}", shift);
            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.Shift == shift)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMEmployees>> GetAbsentEmployeesByDateAsync(DateTime date)
        {
            _logger.LogDebug("Retrieving absent employees for date: {Date}", date.ToString("yyyy-MM-dd"));

            var employeesWithAttendance = await _context.EMAttendance
                .Where(a => a.AttendanceDate.Date == date.Date)
                .Select(a => a.EmployeeId)
                .ToListAsync();

            return await _context.EMEmployees
                .Where(e => !employeesWithAttendance.Contains(e.Id))
                .OrderBy(e => e.LastName)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalHoursWorkedByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Calculating total hours for employee {EmployeeId} from {StartDate} to {EndDate}",
                employeeId, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var attendances = await _context.EMAttendance
                .Where(a => a.EmployeeId == employeeId &&
                           a.AttendanceDate >= startDate.Date &&
                           a.AttendanceDate <= endDate.Date &&
                           a.HoursWorked.HasValue)
                .Select(a => a.HoursWorked.Value)
                .ToListAsync();

            return attendances.Sum();
        }

        public async Task<decimal> GetTotalOvertimeByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Calculating overtime for employee {EmployeeId} from {StartDate} to {EndDate}",
                employeeId, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var attendances = await _context.EMAttendance
                .Where(a => a.EmployeeId == employeeId &&
                           a.AttendanceDate >= startDate.Date &&
                           a.AttendanceDate <= endDate.Date &&
                           a.OvertimeHours.HasValue)
                .Select(a => a.OvertimeHours.Value)
                .ToListAsync();

            return attendances.Sum();
        }

        public async Task<IEnumerable<EMAttendance>> GetLateArrivalsByDateAsync(DateTime date, TimeSpan lateThreshold)
        {
            _logger.LogDebug("Retrieving late arrivals for date: {Date} after {Threshold}",
                date.ToString("yyyy-MM-dd"), lateThreshold.ToString(@"hh\:mm"));

            return await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate.Date == date.Date &&
                           a.TimeIn.HasValue &&
                           a.TimeIn.Value > lateThreshold)
                .OrderBy(a => a.TimeIn)
                .ToListAsync();
        }

        public async Task<object> GetDepartmentAttendanceStatisticsAsync(string department, DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Calculating attendance statistics for department {Department} from {StartDate} to {EndDate}",
                department, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var attendances = await _context.EMAttendance
                .Include(a => a.Employee)
                .Where(a => a.Employee.Department == department &&
                           a.AttendanceDate >= startDate.Date &&
                           a.AttendanceDate <= endDate.Date)
                .ToListAsync();

            return new
            {
                Department = department,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalRecords = attendances.Count,
                PresentCount = attendances.Count(a => a.Status == "Present"),
                AbsentCount = attendances.Count(a => a.Status == "Absent"),
                LateCount = attendances.Count(a => a.Status == "Late"),
                AverageHoursWorked = attendances.Where(a => a.HoursWorked.HasValue).Average(a => a.HoursWorked),
                TotalOvertime = attendances.Sum(a => a.OvertimeHours ?? 0),
                EmployeesWithAttendance = attendances.Select(a => a.EmployeeId).Distinct().Count()
            };
        }
    }
}
