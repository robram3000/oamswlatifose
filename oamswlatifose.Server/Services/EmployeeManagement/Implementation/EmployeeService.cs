using AutoMapper;
using oamswlatifose.Server.DTO.Employee;
using oamswlatifose.Server.Repository.EmployeeManagement.Interface;
using oamswlatifose.Server.Services.EmployeeManagement.Interfaces;
using System.Text;

namespace oamswlatifose.Server.Services.EmployeeManagement.Implementation
{
    /// <summary>
    /// Comprehensive employee management service implementing business logic, validation,
    /// and orchestration between repositories, mappers, and external systems.
    /// </summary>
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly IEmployeeManagementQueryRepository _queryRepository;
        private readonly IEmployeeManagementCommandRepository _commandRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateEmployeeDTO> _createValidator;
        private readonly IValidator<UpdateEmployeeDTO> _updateValidator;

        public EmployeeService(
            IEmployeeManagementQueryRepository queryRepository,
            IEmployeeManagementCommandRepository commandRepository,
            IMapper mapper,
            IValidator<CreateEmployeeDTO> createValidator,
            IValidator<UpdateEmployeeDTO> updateValidator,
            ILogger<EmployeeService> logger,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationIdGenerator correlationIdGenerator)
            : base(logger, httpContextAccessor, correlationIdGenerator)
        {
            _queryRepository = queryRepository;
            _commandRepository = commandRepository;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<ServiceResponse<PagedResult<EmployeeSummaryDTO>>> GetAllEmployeesAsync(int pageNumber, int pageSize)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employees = await _queryRepository.GetEmployeesPaginatedAsync(pageNumber, pageSize);
                    var totalCount = await _queryRepository.GetTotalEmployeeCountAsync();

                    var employeeDtos = _mapper.Map<IEnumerable<EmployeeSummaryDTO>>(employees);

                    var result = new PagedResult<EmployeeSummaryDTO>
                    {
                        Items = employeeDtos,
                        TotalCount = totalCount,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };

                    return ServiceResponse<PagedResult<EmployeeSummaryDTO>>.Success(
                        result,
                        $"Retrieved {employeeDtos.Count()} of {totalCount} employees");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving employees");
                    return ServiceResponse<PagedResult<EmployeeSummaryDTO>>.FromException(
                        ex, "Failed to retrieve employees");
                }
            }, "GetAllEmployeesAsync");
        }

        public async Task<ServiceResponse<EmployeeResponseDTO>> GetEmployeeByIdAsync(int id)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employee = await _queryRepository.GetEmployeeByIdAsync(id);

                    if (employee == null)
                        return ServiceResponse<EmployeeResponseDTO>.Failure($"Employee with ID {id} not found");

                    var employeeDto = _mapper.Map<EmployeeResponseDTO>(employee);
                    return ServiceResponse<EmployeeResponseDTO>.Success(employeeDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
                    return ServiceResponse<EmployeeResponseDTO>.FromException(
                        ex, $"Failed to retrieve employee with ID {id}");
                }
            }, "GetEmployeeByIdAsync");
        }

        public async Task<ServiceResponse<EmployeeResponseDTO>> GetEmployeeByEmployeeIdAsync(int employeeId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employee = await _queryRepository.GetEmployeeByEmployeeIdAsync(employeeId);

                    if (employee == null)
                        return ServiceResponse<EmployeeResponseDTO>.Failure($"Employee with EmployeeID {employeeId} not found");

                    var employeeDto = _mapper.Map<EmployeeResponseDTO>(employee);
                    return ServiceResponse<EmployeeResponseDTO>.Success(employeeDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving employee by EmployeeID {EmployeeId}", employeeId);
                    return ServiceResponse<EmployeeResponseDTO>.FromException(
                        ex, $"Failed to retrieve employee with EmployeeID {employeeId}");
                }
            }, "GetEmployeeByEmployeeIdAsync");
        }

        public async Task<ServiceResponse<EmployeeResponseDTO>> CreateEmployeeAsync(CreateEmployeeDTO createDto)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate DTO
                    var validationResult = await _createValidator.ValidateAsync(createDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<EmployeeResponseDTO>.Failure(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Check for duplicate email
                    var existingEmail = await _queryRepository.GetEmployeeByEmailAsync(createDto.Email);
                    if (existingEmail != null)
                    {
                        return ServiceResponse<EmployeeResponseDTO>.Failure(
                            $"Employee with email {createDto.Email} already exists");
                    }

                    // Map and create
                    var employee = _mapper.Map<EMEmployees>(createDto);
                    var created = await _commandRepository.CreateEmployeeAsync(employee);

                    var employeeDto = _mapper.Map<EmployeeResponseDTO>(created);

                    _logger.LogInformation("Employee created successfully: {EmployeeId}, ID: {Id}",
                        created.EmployeeID, created.Id);

                    return ServiceResponse<EmployeeResponseDTO>.Success(
                        employeeDto,
                        "Employee created successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating employee");
                    return ServiceResponse<EmployeeResponseDTO>.FromException(
                        ex, "Failed to create employee");
                }
            }, "CreateEmployeeAsync");
        }

        public async Task<ServiceResponse<EmployeeResponseDTO>> UpdateEmployeeAsync(int id, UpdateEmployeeDTO updateDto)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate DTO
                    var validationResult = await _updateValidator.ValidateAsync(updateDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<EmployeeResponseDTO>.Failure(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Check if employee exists
                    var existingEmployee = await _queryRepository.GetEmployeeByIdAsync(id);
                    if (existingEmployee == null)
                    {
                        return ServiceResponse<EmployeeResponseDTO>.Failure($"Employee with ID {id} not found");
                    }

                    // Check email uniqueness if changed
                    if (!string.IsNullOrEmpty(updateDto.Email) &&
                        updateDto.Email != existingEmployee.Email)
                    {
                        var duplicateEmail = await _queryRepository.GetEmployeeByEmailAsync(updateDto.Email);
                        if (duplicateEmail != null && duplicateEmail.Id != id)
                        {
                            return ServiceResponse<EmployeeResponseDTO>.Failure(
                                $"Employee with email {updateDto.Email} already exists");
                        }
                    }

                    // Map updates to existing entity
                    _mapper.Map(updateDto, existingEmployee);

                    var updated = await _commandRepository.UpdateEmployeeAsync(existingEmployee);
                    var employeeDto = _mapper.Map<EmployeeResponseDTO>(updated);

                    _logger.LogInformation("Employee updated successfully: ID: {Id}", id);

                    return ServiceResponse<EmployeeResponseDTO>.Success(
                        employeeDto,
                        "Employee updated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
                    return ServiceResponse<EmployeeResponseDTO>.FromException(
                        ex, $"Failed to update employee with ID {id}");
                }
            }, "UpdateEmployeeAsync");
        }

        public async Task<ServiceResponse<bool>> DeleteEmployeeAsync(int id)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var exists = await _queryRepository.EmployeeExistsAsync(id);
                    if (!exists)
                    {
                        return ServiceResponse<bool>.Failure($"Employee with ID {id} not found");
                    }

                    var result = await _commandRepository.DeleteEmployeeAsync(id);

                    if (result)
                    {
                        _logger.LogInformation("Employee deleted successfully: ID: {Id}", id);
                        return ServiceResponse<bool>.Success(true, "Employee deleted successfully");
                    }

                    return ServiceResponse<bool>.Failure("Failed to delete employee");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                    return ServiceResponse<bool>.FromException(
                        ex, $"Failed to delete employee with ID {id}");
                }
            }, "DeleteEmployeeAsync");
        }

        public async Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> SearchEmployeesAsync(string searchTerm)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employees = await _queryRepository.SearchEmployeesAsync(searchTerm);
                    var employeeDtos = _mapper.Map<IEnumerable<EmployeeSummaryDTO>>(employees);

                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.Success(
                        employeeDtos,
                        $"Found {employeeDtos.Count()} employees matching '{searchTerm}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching employees with term: {SearchTerm}", searchTerm);
                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.FromException(
                        ex, "Failed to search employees");
                }
            }, "SearchEmployeesAsync");
        }

        public async Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> GetEmployeesByDepartmentAsync(string department)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employees = await _queryRepository.GetEmployeesByDepartmentAsync(department);
                    var employeeDtos = _mapper.Map<IEnumerable<EmployeeSummaryDTO>>(employees);

                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.Success(
                        employeeDtos,
                        $"Found {employeeDtos.Count()} employees in department '{department}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving employees in department: {Department}", department);
                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.FromException(
                        ex, "Failed to retrieve employees by department");
                }
            }, "GetEmployeesByDepartmentAsync");
        }

        public async Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> GetEmployeesByPositionAsync(string position)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employees = await _queryRepository.GetEmployeesByPositionAsync(position);
                    var employeeDtos = _mapper.Map<IEnumerable<EmployeeSummaryDTO>>(employees);

                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.Success(
                        employeeDtos,
                        $"Found {employeeDtos.Count()} employees with position '{position}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving employees with position: {Position}", position);
                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.FromException(
                        ex, "Failed to retrieve employees by position");
                }
            }, "GetEmployeesByPositionAsync");
        }

        public async Task<ServiceResponse<IEnumerable<EmployeeSummaryDTO>>> GetEmployeesWithoutUserAccountsAsync()
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var allEmployees = await _queryRepository.GetAllEmployeesAsync();
                    var employeesWithoutAccounts = allEmployees.Where(e => e.UserAccount == null);
                    var employeeDtos = _mapper.Map<IEnumerable<EmployeeSummaryDTO>>(employeesWithoutAccounts);

                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.Success(
                        employeeDtos,
                        $"Found {employeeDtos.Count()} employees without user accounts");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving employees without user accounts");
                    return ServiceResponse<IEnumerable<EmployeeSummaryDTO>>.FromException(
                        ex, "Failed to retrieve employees without user accounts");
                }
            }, "GetEmployeesWithoutUserAccountsAsync");
        }

        public async Task<ServiceResponse<DepartmentStatisticsDTO>> GetDepartmentStatisticsAsync()
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var allEmployees = await _queryRepository.GetAllEmployeesAsync();

                    var stats = new DepartmentStatisticsDTO
                    {
                        TotalEmployees = allEmployees.Count(),
                        Departments = allEmployees
                            .GroupBy(e => e.Department ?? "Unassigned")
                            .Select(g => new DepartmentStatDTO
                            {
                                DepartmentName = g.Key,
                                EmployeeCount = g.Count(),
                                UniquePositions = g.Select(e => e.Position).Distinct().Count(),
                                HasUserAccounts = g.Count(e => e.UserAccount != null)
                            })
                            .OrderByDescending(d => d.EmployeeCount)
                            .ToList()
                    };

                    return ServiceResponse<DepartmentStatisticsDTO>.Success(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving department statistics");
                    return ServiceResponse<DepartmentStatisticsDTO>.FromException(
                        ex, "Failed to retrieve department statistics");
                }
            }, "GetDepartmentStatisticsAsync");
        }

        public async Task<ServiceResponse<byte[]>> ExportEmployeesAsync(string format = "excel")
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var employees = await _queryRepository.GetAllEmployeesAsync();
                    var employeeDtos = _mapper.Map<IEnumerable<EmployeeSummaryDTO>>(employees);

                    byte[] exportData;
                    string contentType;

                    switch (format.ToLower())
                    {
                        case "csv":
                            exportData = GenerateCsvExport(employeeDtos);
                            contentType = "text/csv";
                            break;
                        case "json":
                            exportData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(employeeDtos, new JsonSerializerOptions { WriteIndented = true }));
                            contentType = "application/json";
                            break;
                        case "excel":
                        default:
                            exportData = GenerateExcelExport(employeeDtos);
                            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            break;
                    }

                    return ServiceResponse<byte[]>.Success(exportData, $"Export generated successfully in {format} format");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting employees to {Format}", format);
                    return ServiceResponse<byte[]>.FromException(ex, $"Failed to export employees to {format}");
                }
            }, "ExportEmployeesAsync");
        }

        private byte[] GenerateCsvExport(IEnumerable<EmployeeSummaryDTO> employees)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EmployeeID,FullName,Email,Department,Position");

            foreach (var emp in employees)
            {
                sb.AppendLine($"{emp.EmployeeID},{emp.FullName},{emp.Email},{emp.Department},{emp.Position}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private byte[] GenerateExcelExport(IEnumerable<EmployeeSummaryDTO> employees)
        {
            // Note: In production, use EPPlus or ClosedXML for proper Excel generation
            var csv = GenerateCsvExport(employees);
            return csv; // Placeholder - implement proper Excel generation
        }
    }

}
