using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Repository.EmployeeManagement.Interface
{
    /// <summary>
    /// Interface for employee data query operations providing comprehensive read-only access to employee information.
    /// This repository interface defines contract methods for retrieving employee records with various filtering,
    /// searching, and pagination capabilities essential for the employee management module.
    /// 
    /// <para>Core Responsibilities:</para>
    /// <para>- Retrieve employee details by various identifiers (ID, EmployeeID, Email)</para>
    /// <para>- Implement advanced filtering and search functionality for employee records</para>
    /// <para>- Support paginated employee listings with sorting capabilities</para>
    /// <para>- Provide department and position-based employee queries</para>
    /// <para>- Enable employee existence verification operations</para>
    /// 
    /// <para>All methods are asynchronous and follow the async/await pattern to ensure
    /// non-blocking database operations and optimal application performance.</para>
    /// </summary>
    public interface IEmployeeManagementQueryRepository
    {
        /// <summary>
        /// Retrieves all employee records from the system asynchronously.
        /// This method provides a complete list of all employees without any filtering,
        /// suitable for administrative views and comprehensive reporting scenarios.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with collection of all EMEmployees entities</returns>
        Task<IEnumerable<EMEmployees>> GetAllEmployeesAsync();

        /// <summary>
        /// Retrieves a paginated list of employee records for efficient data display and navigation.
        /// Implements server-side pagination to optimize performance when dealing with large employee datasets.
        /// </summary>
        /// <param name="pageNumber">The current page number (1-indexed, must be greater than 0)</param>
        /// <param name="pageSize">The number of employee records to display per page (1-100 range recommended)</param>
        /// <returns>A task with paginated employee results containing the specified page of employee records</returns>
        Task<IEnumerable<EMEmployees>> GetEmployeesPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves detailed employee information using the unique system-generated identifier.
        /// This method provides complete employee profile data for detailed views and editing operations.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the employee record</param>
        /// <returns>A task containing the employee entity if found; otherwise, null reference</returns>
        Task<EMEmployees> GetEmployeeByIdAsync(int id);

        /// <summary>
        /// Retrieves employee information using the business-specific Employee ID number.
        /// This method supports legacy system integration and employee self-service portals.
        /// </summary>
        /// <param name="employeeId">The business-specific employee identifier (e.g., employee badge number)</param>
        /// <returns>A task containing the employee entity if found with matching EmployeeID; otherwise, null</returns>
        Task<EMEmployees> GetEmployeeByEmployeeIdAsync(int employeeId);

        /// <summary>
        /// Retrieves employee information using the corporate email address.
        /// Essential for authentication flows, communication systems, and directory services integration.
        /// </summary>
        /// <param name="email">The employee's corporate email address (case-insensitive comparison)</param>
        /// <returns>A task containing the employee entity if found with matching email; otherwise, null</returns>
        Task<EMEmployees> GetEmployeeByEmailAsync(string email);

        /// <summary>
        /// Retrieves all employees belonging to a specific department for organizational reporting.
        /// Supports departmental analytics, headcount reporting, and team management views.
        /// </summary>
        /// <param name="department">The department name to filter employees by</param>
        /// <returns>A task containing collection of employees associated with the specified department</returns>
        Task<IEnumerable<EMEmployees>> GetEmployeesByDepartmentAsync(string department);

        /// <summary>
        /// Retrieves all employees holding a specific job position for role-based analysis.
        /// Useful for position-based reporting, salary benchmarking, and organizational structure analysis.
        /// </summary>
        /// <param name="position">The job position title to filter employees by</param>
        /// <returns>A task containing collection of employees with the specified job position</returns>
        Task<IEnumerable<EMEmployees>> GetEmployeesByPositionAsync(string position);

        /// <summary>
        /// Performs a comprehensive search across employee records using multiple criteria.
        /// Implements fuzzy matching across first name, last name, email, and other employee fields.
        /// </summary>
        /// <param name="searchTerm">The search term to match against employee attributes</param>
        /// <returns>A task containing collection of employees matching the specified search criteria</returns>
        Task<IEnumerable<EMEmployees>> SearchEmployeesAsync(string searchTerm);

        /// <summary>
        /// Verifies if an employee record exists in the system using the unique identifier.
        /// Used for validation operations before performing update or delete actions.
        /// </summary>
        /// <param name="id">The unique system identifier of the employee to verify</param>
        /// <returns>A task containing boolean indicating whether the employee exists</returns>
        Task<bool> EmployeeExistsAsync(int id);

        /// <summary>
        /// Retrieves the total count of active employee records in the system.
        /// Essential for dashboard metrics, reporting, and pagination total count calculations.
        /// </summary>
        /// <returns>A task containing the total number of employee records</returns>
        Task<int> GetTotalEmployeeCountAsync();
    }
}

