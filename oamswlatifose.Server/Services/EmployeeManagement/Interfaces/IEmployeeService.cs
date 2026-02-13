using oamswlatifose.Server.DTO.Employee;

namespace oamswlatifose.Server.Services.EmployeeManagement.Interfaces
{
    /// <summary>
    /// Service interface for employee management operations providing comprehensive business logic
    /// for employee data management, validation, and reporting. This service orchestrates between
    /// repositories, applies business rules, and returns standardized responses.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Complete CRUD operations with business rule validation</para>
    /// <para>- Employee search and filtering with pagination</para>
    /// <para>- Department and position-based reporting</para>
    /// <para>- Employee-user account synchronization</para>
    /// <para>- Bulk import/export operations</para>
    /// <para>- Attendance summary and analytics</para>
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>
        /// Retrieves all employees with pagination support.
        /// </summary>
        Task<ServiceResponse<PagedResult<EmployeeSummaryDTO>>> GetAllEmployeesAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves detailed employee information by ID.
        /// </summary>
        Task<ServiceResponse<EmployeeResponseDTO>> GetEmployeeByIdAsync(int id);

        /// <summary>
        /// Retrieves employee by employee badge number.
        /// </summary>
        Task<ServiceResponse<EmployeeResponseDTO>> GetEmployeeByEmployeeIdAsync(int employeeId);

        /// <summary>
        /// Creates a new employee record with validation.
        /// </summary>
        Task<ServiceResponse<EmployeeResponseDTO>> CreateEmployeeAsync(CreateEmployeeDTO createDto);

        /// <summary>
        /// Updates an existing employee record.
        /// </summary>
        Task<ServiceResponse<EmployeeResponseDTO>> UpdateEmployeeAsync(int id, UpdateEmployeeDTO updateDto);

        /// <summary>
        /// Deletes an employee record.
        /// </summary>
        Task<ServiceResponse<bool>> DeleteEmployeeAsync(int id);

        /// <summary>
        /// Searches employees by criteria.
        /// </summary>
        Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> SearchEmployeesAsync(string searchTerm);

        /// <summary>
        /// Gets employees by department.
        /// </summary>
        Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> GetEmployeesByDepartmentAsync(string department);

        /// <summary>
        /// Gets employees by position.
        /// </summary>
        Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> GetEmployeesByPositionAsync(string position);

        /// <summary>
        /// Gets employees without user accounts.
        /// </summary>
        Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> GetEmployeesWithoutUserAccountsAsync();

        /// <summary>
        /// Gets department statistics.
        /// </summary>
        Task<ServiceResponse<DepartmentStatisticsDTO>> GetDepartmentStatisticsAsync();

        /// <summary>
        /// Exports employees to specified format.
        /// </summary>
        Task<ServiceResponse<byte[]>> ExportEmployeesAsync(string format = "excel");
    }
}
