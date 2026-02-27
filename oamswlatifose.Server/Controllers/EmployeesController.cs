using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.Controllers;
using oamswlatifose.Server.DTO.Employee;
using oamswlatifose.Server.DTO.User;
using oamswlatifose.Server.Services;
using oamswlatifose.Server.Services.EmployeeManagement.Interfaces;
using oamswlatifose.Server.Utilities.Security;
using System.Security.Claims;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// API controller for employee management operations.
    /// Provides endpoints for CRUD operations, searching, and reporting.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class EmployeesController : BaseApiController
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// Gets paginated list of all employees.
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <returns>Paginated list of employees</returns>
        [HttpGet]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<EmployeeSummaryDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<EmployeeSummaryDTO>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<EmployeeSummaryDTO>>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployees([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageSize = Math.Min(pageSize, 100);
            var result = await _employeeService.GetAllEmployeesAsync(pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Gets employee by ID.
        /// </summary>
        /// <param name="id">Employee system ID</param>
        /// <returns>Employee details</returns>
        [HttpGet("{id}")]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var result = await _employeeService.GetEmployeeByIdAsync(id);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Gets employee by employee badge number.
        /// </summary>
        /// <param name="employeeId">Employee badge number</param>
        /// <returns>Employee details</returns>
        [HttpGet("badge/{employeeId}")]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeByBadge(int employeeId)
        {
            var result = await _employeeService.GetEmployeeByEmployeeIdAsync(employeeId);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new employee.
        /// </summary>
        /// <param name="createDto">Employee creation data</param>
        /// <returns>Created employee</returns>
        [HttpPost]
        [PermissionAuthorize("edit_employees")]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDTO createDto)
        {
            var result = await _employeeService.CreateEmployeeAsync(createDto);

            if (!result.IsSuccess)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetEmployee), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Updates an existing employee.
        /// </summary>
        /// <param name="id">Employee ID to update</param>
        /// <param name="updateDto">Employee update data</param>
        /// <returns>Updated employee</returns>
        [HttpPut("{id}")]
        [PermissionAuthorize("edit_employees")]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeResponseDTO>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDTO updateDto)
        {
            var result = await _employeeService.UpdateEmployeeAsync(id, updateDto);

            if (!result.IsSuccess)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Deletes an employee.
        /// </summary>
        /// <param name="id">Employee ID to delete</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [PermissionAuthorize("delete_employees")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var result = await _employeeService.DeleteEmployeeAsync(id);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Searches employees by term.
        /// </summary>
        /// <param name="term">Search term</param>
        /// <returns>Matching employees</returns>
        [HttpGet("search")]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<EmployeeSummaryDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchEmployees([FromQuery] string term)
        {
            var result = await _employeeService.SearchEmployeesAsync(term);
            return Ok(result);
        }

        /// <summary>
        /// Gets employees by department.
        /// </summary>
        /// <param name="department">Department name</param>
        /// <returns>Employees in department</returns>
        [HttpGet("department/{department}")]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<EmployeeSummaryDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployeesByDepartment(string department)
        {
            var result = await _employeeService.GetEmployeesByDepartmentAsync(department);
            return Ok(result);
        }

        /// <summary>
        /// Gets employees by position.
        /// </summary>
        /// <param name="position">Position title</param>
        /// <returns>Employees with position</returns>
        [HttpGet("position/{position}")]
        [PermissionAuthorize("view_employees")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<EmployeeSummaryDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployeesByPosition(string position)
        {
            var result = await _employeeService.GetEmployeesByPositionAsync(position);
            return Ok(result);
        }

        /// <summary>
        /// Gets employees without user accounts.
        /// </summary>
        /// <returns>Employees without user accounts</returns>
        [HttpGet("no-account")]
        [PermissionAuthorize("manage_users")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<EmployeeSummaryDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployeesWithoutAccounts()
        {
            var result = await _employeeService.GetEmployeesWithoutUserAccountsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets department statistics.
        /// </summary>
        /// <returns>Department statistics</returns>
        [HttpGet("statistics/departments")]
        [PermissionAuthorize("generate_reports")]
        [ProducesResponseType(typeof(ServiceResponse<DepartmentStatisticsDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartmentStatistics()
        {
            var result = await _employeeService.GetDepartmentStatisticsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Exports employees to specified format.
        /// </summary>
        /// <param name="format">Export format (excel, csv, json)</param>
        /// <returns>Exported file</returns>
        [HttpGet("export")]
        [PermissionAuthorize("generate_reports")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportEmployees([FromQuery] string format = "excel")
        {
            var result = await _employeeService.ExportEmployeesAsync(format);

            if (!result.IsSuccess)
                return BadRequest(result);

            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "json" => "application/json",
                _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            var fileName = $"employees_export_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";

            return File(result.Data, contentType, fileName);
        }
    }
}
