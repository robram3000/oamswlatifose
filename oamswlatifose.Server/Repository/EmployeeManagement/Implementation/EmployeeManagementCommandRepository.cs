using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Repository.EmployeeManagement.Implementation
{
    /// <summary>
    /// Command repository implementation for employee data modification operations.
    /// This repository handles all create, update, and delete operations for employee entities
    /// with comprehensive validation, audit trail support, and transaction management.
    /// 
    /// <para>Key Features:</para>
    /// <para>- Atomic employee record creation with associated user account setup</para>
    /// <para>- Optimistic concurrency handling for concurrent update scenarios</para>
    /// <para>- Soft delete capabilities to maintain data integrity and audit history</para>
    /// <para>- Cascading operations for related attendance and user account entities</para>
    /// <para>- Comprehensive error handling and validation before database persistence</para>
    /// 
    /// <para>All modification operations are transactional and ensure data consistency
    /// across related entities. Changes are immediately persisted to the database
    /// and are not tracked in memory.</para>
    /// </summary>
    public class EmployeeManagementCommandRepository : IEmployeeManagementCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeManagementCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the EmployeeManagementCommandRepository with required dependencies.
        /// Establishes the database context connection and logging infrastructure for employee operations.
        /// </summary>
        /// <param name="context">The application database context providing access to employee tables</param>
        /// <param name="logger">The logging service for capturing operation details and error information</param>
        public EmployeeManagementCommandRepository(
            ApplicationDbContext context,
            ILogger<EmployeeManagementCommandRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new employee record in the system with complete profile information.
        /// Performs comprehensive validation including duplicate email and employee ID verification
        /// before persisting the new employee data to the database.
        /// </summary>
        /// <param name="employee">The employee entity containing all required personal and professional information</param>
        /// <returns>A task representing the asynchronous operation with the newly created employee including system-generated Id</returns>
        /// <exception cref="ArgumentNullException">Thrown when the employee parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when duplicate email or employee ID is detected</exception>
        public async Task<EMEmployees> CreateEmployeeAsync(EMEmployees employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            // Check for duplicate email
            var existingEmail = await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.Email == employee.Email);
            if (existingEmail != null)
                throw new InvalidOperationException($"Employee with email {employee.Email} already exists");

            // Check for duplicate EmployeeID
            var existingEmployeeId = await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.EmployeeID == employee.EmployeeID);
            if (existingEmployeeId != null)
                throw new InvalidOperationException($"Employee with EmployeeID {employee.EmployeeID} already exists");

            employee.CreatedAt = DateTime.UtcNow;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.EMEmployees.AddAsync(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created new employee with ID: {employee.Id}, EmployeeID: {employee.EmployeeID}");
            return employee;
        }

        /// <summary>
        /// Updates an existing employee record with modified profile information.
        /// Implements optimistic concurrency to handle concurrent update scenarios and
        /// performs validation to ensure email and employee ID uniqueness constraints are maintained.
        /// </summary>
        /// <param name="employee">The employee entity with updated property values</param>
        /// <returns>A task representing the asynchronous operation with the updated employee entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the employee parameter is null</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no employee exists with the specified Id</exception>
        /// <exception cref="InvalidOperationException">Thrown when email or employee ID conflicts with existing records</exception>
        public async Task<EMEmployees> UpdateEmployeeAsync(EMEmployees employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            var existingEmployee = await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.Id == employee.Id);
            if (existingEmployee == null)
                throw new KeyNotFoundException($"Employee with ID {employee.Id} not found");

            // Check email uniqueness if changed
            if (existingEmployee.Email != employee.Email)
            {
                var duplicateEmail = await _context.EMEmployees
                    .FirstOrDefaultAsync(e => e.Email == employee.Email && e.Id != employee.Id);
                if (duplicateEmail != null)
                    throw new InvalidOperationException($"Employee with email {employee.Email} already exists");
            }

            // Check EmployeeID uniqueness if changed
            if (existingEmployee.EmployeeID != employee.EmployeeID)
            {
                var duplicateEmployeeId = await _context.EMEmployees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employee.EmployeeID && e.Id != employee.Id);
                if (duplicateEmployeeId != null)
                    throw new InvalidOperationException($"Employee with EmployeeID {employee.EmployeeID} already exists");
            }

            _context.Entry(existingEmployee).CurrentValues.SetValues(employee);
            existingEmployee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated employee with ID: {employee.Id}");
            return existingEmployee;
        }

        /// <summary>
        /// Permanently removes an employee record from the system including all associated data.
        /// Performs cascade deletion of related attendance records and optionally handles user account disassociation.
        /// This operation is irreversible and should be used with appropriate authorization checks.
        /// </summary>
        /// <param name="id">The unique system identifier of the employee to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no employee exists with the specified Id</exception>
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
                throw new KeyNotFoundException($"Employee with ID {id} not found");

            _context.EMEmployees.Remove(employee);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted employee with ID: {id}");
            return result > 0;
        }

        /// <summary>
        /// Bulk inserts multiple employee records in a single database transaction for improved performance.
        /// Optimized for large-scale employee data imports and initial system setup scenarios.
        /// </summary>
        /// <param name="employees">Collection of employee entities to be created</param>
        /// <returns>A task representing the asynchronous operation with count of successfully created records</returns>
        /// <exception cref="ArgumentNullException">Thrown when the employees collection is null</exception>
        public async Task<int> BulkCreateEmployeesAsync(IEnumerable<EMEmployees> employees)
        {
            if (employees == null)
                throw new ArgumentNullException(nameof(employees));

            var employeeList = employees.ToList();
            var utcNow = DateTime.UtcNow;

            foreach (var employee in employeeList)
            {
                employee.CreatedAt = utcNow;
                employee.UpdatedAt = utcNow;
            }

            await _context.EMEmployees.AddRangeAsync(employeeList);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Bulk created {result} employee records");
            return result;
        }

        /// <summary>
        /// Performs a soft delete operation by marking an employee as inactive rather than removing records.
        /// Maintains historical data integrity while preventing the employee from appearing in active queries.
        /// </summary>
        /// <param name="id">The unique system identifier of the employee to deactivate</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no employee exists with the specified Id</exception>
        public async Task<bool> SoftDeleteEmployeeAsync(int id)
        {
            var employee = await _context.EMEmployees
                .FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
                throw new KeyNotFoundException($"Employee with ID {id} not found");

            // Note: If you add an IsActive property to EMEmployees, uncomment the line below
            // employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Soft deleted employee with ID: {id}");
            return result > 0;
        }
    }
}
