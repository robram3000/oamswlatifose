using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Repository.EmployeeManagement.Interface
{
    /// <summary>
    /// Interface for employee data modification operations defining contracts for all create,
    /// update, and delete operations on employee entities. This repository interface establishes
    /// the pattern for employee data persistence with comprehensive business rule enforcement.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Single record creation with complete employee profile data</para>
    /// <para>- Partial and full employee record updates with concurrency handling</para>
    /// <para>- Permanent removal operations with cascade behavior options</para>
    /// <para>- Bulk operations for efficient mass data processing</para>
    /// <para>- Soft delete capabilities for non-destructive removal</para>
    /// 
    /// <para>All methods enforce data integrity rules, maintain audit timestamps,
    /// and ensure proper validation before database persistence. Implementations
    /// must handle concurrency conflicts and provide appropriate error feedback.</para>
    /// </summary>
    public interface IEmployeeManagementCommandRepository
    {
        /// <summary>
        /// Creates a new employee record in the system with validation and audit trail.
        /// Ensures unique constraints for email and employee ID before persisting the entity.
        /// </summary>
        /// <param name="employee">The employee entity containing all required personal and professional information</param>
        /// <returns>A task representing the asynchronous operation with the newly created employee including system-generated identifier</returns>
        Task<EMEmployees> CreateEmployeeAsync(EMEmployees employee);

        /// <summary>
        /// Updates an existing employee record with modified information while maintaining data integrity.
        /// Performs optimistic concurrency checking and validates unique constraints before applying changes.
        /// </summary>
        /// <param name="employee">The employee entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated employee entity</returns>
        Task<EMEmployees> UpdateEmployeeAsync(EMEmployees employee);

        /// <summary>
        /// Permanently removes an employee record and all associated dependent entities from the system.
        /// This operation is irreversible and triggers cascade deletes for related attendance records.
        /// </summary>
        /// <param name="id">The unique system identifier of the employee to permanently delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> DeleteEmployeeAsync(int id);

        /// <summary>
        /// Efficiently creates multiple employee records in a single transactional operation.
        /// Optimized for bulk data imports, system migrations, and large-scale employee onboarding.
        /// </summary>
        /// <param name="employees">Collection of employee entities to be created in the database</param>
        /// <returns>A task representing the asynchronous operation with the count of successfully created records</returns>
        Task<int> BulkCreateEmployeesAsync(IEnumerable<EMEmployees> employees);

        /// <summary>
        /// Performs a non-destructive removal by marking an employee as inactive while preserving all historical data.
        /// The employee record remains in the database but is excluded from standard query operations.
        /// </summary>
        /// <param name="id">The unique system identifier of the employee to soft delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> SoftDeleteEmployeeAsync(int id);
    }
}
