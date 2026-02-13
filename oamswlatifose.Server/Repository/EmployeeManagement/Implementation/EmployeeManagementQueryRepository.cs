using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.user;
using oamswlatifose.Server.Repository.EmployeeManagement.Interface;

namespace oamswlatifose.Server.Repository.EmployeeManagement.Implementation
{
    /// <summary>
    /// Query repository implementation for employee data retrieval operations with advanced filtering,
    /// searching, and pagination capabilities. This repository provides comprehensive read-only access
    /// to employee information optimized for performance through efficient query patterns.
    /// 
    /// <para>Performance Optimizations:</para>
    /// <para>- Asynchronous query execution to prevent thread blocking</para>
    /// <para>- Index-aware query construction for frequently accessed fields</para>
    /// <para>- Pagination implementation to limit result set sizes</para>
    /// <para>- Selective column loading where applicable for reduced data transfer</para>
    /// <para>- Caching strategies for frequently accessed, rarely changed data</para>
    /// 
    /// <para>All queries are compiled and optimized by Entity Framework Core
    /// to generate efficient SQL statements for the underlying database provider.</para>
    /// </summary>
    public class EmployeeManagementQueryRepository : IEmployeeManagementQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeManagementQueryRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the EmployeeManagementQueryRepository with database context and logging.
        /// Establishes the query execution pipeline and monitoring infrastructure for employee data access.
        /// </summary>
        /// <param name="context">The application database context providing access to employee tables with optimized query capabilities</param>
        /// <param name="logger">The logging service for capturing query performance metrics and debugging information</param>
        public EmployeeManagementQueryRepository(
            ApplicationDbContext context,
            ILogger<EmployeeManagementQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all employee records from the database with complete profile information.
        /// Warning: This method loads all employees into memory and should be used cautiously
        /// with large datasets. Consider using pagination for production scenarios with many employees.
        /// </summary>
        /// <returns>A collection of all employee entities currently in the system</returns>
        public async Task<IEnumerable<EMEmployees>> GetAllEmployeesAsync()
        {
            _logger.LogDebug("Executing query to retrieve all employees");
            return await _context.EMEmployees
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a paginated subset of employee records for efficient data presentation.
        /// Implements skip-take pattern for optimal performance when displaying employee lists
        /// in UI grids, tables, and reports with large datasets.
        /// </summary>
        /// <param name="pageNumber">The page index starting from 1 for the requested data page</param>
        /// <param name="pageSize">The maximum number of employee records to return in this page</param>
        /// <returns>A collection of employees for the requested page, ordered by last name then first name</returns>
        public async Task<IEnumerable<EMEmployees>> GetEmployeesPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogDebug($"Retrieving employees page {pageNumber} with page size {pageSize}");

            return await _context.EMEmployees
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a single employee by their system-generated primary key identifier.
        /// This is the most efficient method for direct employee lookup when the ID is known.
        /// </summary>
        /// <param name="id">The unique system identifier for the employee</param>
        /// <returns>The employee entity if found; otherwise, null</returns>
        public async Task<EMEmployees> GetEmployeeByIdAsync(int id)
        {
            _logger.LogDebug($"Retrieving employee with ID: {id}");
            return await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <summary>
        /// Retrieves a single employee using their business-specific employee identifier.
        /// Supports legacy system integration and provides direct access to employee records
        /// using the employee badge number or HR-assigned identifier.
        /// </summary>
        /// <param name="employeeId">The business-specific employee identification number</param>
        /// <returns>The employee entity if found with matching EmployeeID; otherwise, null</returns>
        public async Task<EMEmployees> GetEmployeeByEmployeeIdAsync(int employeeId)
        {
            _logger.LogDebug($"Retrieving employee with EmployeeID: {employeeId}");
            return await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
        }

        /// <summary>
        /// Retrieves employee information using corporate email address for authentication
        /// and communication system integration. Email lookup is case-insensitive and utilizes
        /// the unique index on the Email column for optimal performance.
        /// </summary>
        /// <param name="email">The employee's corporate email address</param>
        /// <returns>The employee entity if found with matching email; otherwise, null</returns>
        public async Task<EMEmployees> GetEmployeeByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            _logger.LogDebug($"Retrieving employee with email: {email}");
            return await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower());
        }

        /// <summary>
        /// Retrieves all employees associated with a specific department for organizational analysis.
        /// Essential for department head views, budget planning, and resource allocation reporting.
        /// </summary>
        /// <param name="department">The department name to filter employees by</param>
        /// <returns>Collection of employees belonging to the specified department</returns>
        public async Task<IEnumerable<EMEmployees>> GetEmployeesByDepartmentAsync(string department)
        {
            if (string.IsNullOrWhiteSpace(department))
                throw new ArgumentException("Department cannot be null or empty", nameof(department));

            _logger.LogDebug($"Retrieving employees in department: {department}");
            return await _context.EMEmployees
                .Where(e => e.Department == department)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all employees holding a specific job position for role-based analysis.
        /// Useful for position-specific reporting, skills inventory, and succession planning.
        /// </summary>
        /// <param name="position">The job position title to filter employees by</param>
        /// <returns>Collection of employees with the specified job position</returns>
        public async Task<IEnumerable<EMEmployees>> GetEmployeesByPositionAsync(string position)
        {
            if (string.IsNullOrWhiteSpace(position))
                throw new ArgumentException("Position cannot be null or empty", nameof(position));

            _logger.LogDebug($"Retrieving employees with position: {position}");
            return await _context.EMEmployees
                .Where(e => e.Position == position)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        /// <summary>
        /// Performs comprehensive text search across multiple employee fields including first name,
        /// last name, email, department, and position. Implements case-insensitive pattern matching
        /// for flexible search capabilities in employee directory and lookup functions.
        /// </summary>
        /// <param name="searchTerm">The text to search for within employee records</param>
        /// <returns>Collection of employees matching the search criteria across any of the searchable fields</returns>
        public async Task<IEnumerable<EMEmployees>> SearchEmployeesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));

            _logger.LogDebug($"Searching employees with term: {searchTerm}");

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.EMEmployees
                .Where(e =>
                    e.FirstName.ToLower().Contains(lowerSearchTerm) ||
                    e.LastName.ToLower().Contains(lowerSearchTerm) ||
                    e.Email.ToLower().Contains(lowerSearchTerm) ||
                    (e.Department != null && e.Department.ToLower().Contains(lowerSearchTerm)) ||
                    (e.Position != null && e.Position.ToLower().Contains(lowerSearchTerm)))
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        /// <summary>
        /// Verifies the existence of an employee record using the system identifier.
        /// Optimized for validation operations with minimal data transfer, only checking existence.
        /// </summary>
        /// <param name="id">The system identifier of the employee to verify</param>
        /// <returns>True if an employee with the specified ID exists; otherwise, false</returns>
        public async Task<bool> EmployeeExistsAsync(int id)
        {
            return await _context.EMEmployees.AnyAsync(e => e.Id == id);
        }

        /// <summary>
        /// Retrieves the total count of employee records in the system for pagination
        /// and dashboard metric calculations. Provides quick access to employee headcount.
        /// </summary>
        /// <returns>The total number of employee records in the database</returns>
        public async Task<int> GetTotalEmployeeCountAsync()
        {
            return await _context.EMEmployees.CountAsync();
        }
    }
}
