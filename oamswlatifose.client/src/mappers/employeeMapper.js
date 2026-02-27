/**
 * Employee Data Mapper
 * 
 * This mapper class handles the transformation of employee data between
 * API response formats, frontend display models, and API request formats.
 * It includes security features like masking sensitive data (bank account numbers)
 * and proper date handling.
 * 
 * @class EmployeeMapper
 * @static
 * 
 * @example
 * // Convert API response to frontend employee model
 * const employee = EmployeeMapper.toEmployee(apiResponse.data);
 * 
 * // Create request object for new employee
 * const request = EmployeeMapper.toCreateRequest(formData);
 */
class EmployeeMapper {
  /**
   * Maps raw API employee data to comprehensive frontend model
   * 
   * Transforms API response into a rich frontend model with:
   * - Full name concatenation
   * - Date string to Date object conversions
   * - Sensitive data masking (bank account numbers)
   * - Null handling for optional fields
   * - Reference data (manager info, department IDs, etc.)
   * 
   * @static
   * @param {Object} apiEmployee - Raw employee data from API
   * @param {string|number} apiEmployee.id - Employee record ID
   * @param {string} apiEmployee.employeeId - Employee's badge/employee number
   * @param {string} apiEmployee.firstName - Employee's first name
   * @param {string} apiEmployee.lastName - Employee's last name
   * @param {string} apiEmployee.email - Employee's email address
   * @param {string} [apiEmployee.phoneNumber] - Employee's phone number
   * @param {string} [apiEmployee.dateOfBirth] - Date of birth ISO string
   * @param {string} apiEmployee.hireDate - Hire date ISO string
   * @param {string} [apiEmployee.department] - Department name
   * @param {string} [apiEmployee.position] - Job position title
   * @param {string|number} [apiEmployee.managerId] - Manager's employee ID
   * @param {string} [apiEmployee.managerName] - Manager's full name
   * @param {string} apiEmployee.employmentType - Type of employment
   * @param {string} apiEmployee.employmentStatus - Current employment status
   * @param {string} [apiEmployee.workLocation] - Work location/office
   * @param {Object} [apiEmployee.address] - Employee's address
   * @param {Object} [apiEmployee.emergencyContact] - Emergency contact info
   * @param {string} [apiEmployee.emergencyPhone] - Emergency contact phone
   * @param {string} [apiEmployee.bankAccountNumber] - Bank account (will be masked)
   * @param {string} [apiEmployee.profilePicture] - Profile picture URL
   * @param {string|number} [apiEmployee.departmentId] - Department ID
   * @param {string|number} [apiEmployee.positionId] - Position ID
   * @param {string} apiEmployee.createdAt - Record creation timestamp
   * @param {string} [apiEmployee.updatedAt] - Last update timestamp
   * @returns {Object} Comprehensive employee object for frontend use
   * 
   * @example
   * const apiResponse = {
   *   id: 123,
   *   employeeId: 'EMP001',
   *   firstName: 'John',
   *   lastName: 'Doe',
   *   email: 'john.doe@company.com',
   *   hireDate: '2023-01-15',
   *   bankAccountNumber: '1234567890',
   *   createdAt: '2023-01-15T10:00:00Z'
   * };
   * 
   * const employee = EmployeeMapper.toEmployee(apiResponse);
   * // Returns:
   * // {
   * //   id: 123,
   * //   employeeId: 'EMP001',
   * //   firstName: 'John',
   * //   lastName: 'Doe',
   * //   fullName: 'John Doe',
   * //   email: 'john.doe@company.com',
   * //   bankAccountNumber: '****7890',
   * //   hireDate: Date object,
   * //   createdAt: Date object,
   * //   updatedAt: null,
   * //   ...
   * // }
   */
  static toEmployee(apiEmployee) {
    return {
      id: apiEmployee.id,
      employeeId: apiEmployee.employeeId,
      firstName: apiEmployee.firstName,
      lastName: apiEmployee.lastName,
      fullName: `${apiEmployee.firstName} ${apiEmployee.lastName}`,
      email: apiEmployee.email,
      phoneNumber: apiEmployee.phoneNumber,
      dateOfBirth: apiEmployee.dateOfBirth ? new Date(apiEmployee.dateOfBirth) : null,
      hireDate: new Date(apiEmployee.hireDate),
      department: apiEmployee.department,
      position: apiEmployee.position,
      managerId: apiEmployee.managerId,
      managerName: apiEmployee.managerName,
      employmentType: apiEmployee.employmentType,
      employmentStatus: apiEmployee.employmentStatus,
      workLocation: apiEmployee.workLocation,
      address: apiEmployee.address,
      emergencyContact: apiEmployee.emergencyContact,
      emergencyPhone: apiEmployee.emergencyPhone,
      bankAccountNumber: apiEmployee.bankAccountNumber ? '****' + apiEmployee.bankAccountNumber.slice(-4) : null,
      profilePicture: apiEmployee.profilePicture,
      departmentId: apiEmployee.departmentId,
      positionId: apiEmployee.positionId,
      createdAt: new Date(apiEmployee.createdAt),
      updatedAt: apiEmployee.updatedAt ? new Date(apiEmployee.updatedAt) : null
    };
  }

  /**
   * Maps API employee data to a simplified summary model
   * 
   * Creates a lightweight employee object suitable for lists,
   * dropdowns, and summary displays.
   * 
   * @static
   * @param {Object} apiSummary - Raw employee summary data from API
   * @param {string|number} apiSummary.id - Employee record ID
   * @param {string} apiSummary.employeeId - Employee's badge/employee number
   * @param {string} apiSummary.fullName - Employee's full name
   * @param {string} [apiSummary.department] - Department name
   * @param {string} [apiSummary.position] - Job position title
   * @param {string} apiSummary.employmentStatus - Current employment status
   * @param {string} apiSummary.hireDate - Hire date ISO string
   * @returns {Object} Simplified employee summary object
   * 
   * @example
   * const summary = EmployeeMapper.toSummary({
   *   id: 123,
   *   employeeId: 'EMP001',
   *   fullName: 'John Doe',
   *   department: 'Engineering',
   *   position: 'Developer',
   *   employmentStatus: 'Active',
   *   hireDate: '2023-01-15'
   * });
   * 
   * // Use in employee dropdown
   * <select>
   *   {employees.map(emp => (
   *     <option key={emp.id} value={emp.employeeId}>
   *       {emp.fullName} - {emp.position}
   *     </option>
   *   ))}
   * </select>
   */
  static toSummary(apiSummary) {
    return {
      id: apiSummary.id,
      employeeId: apiSummary.employeeId,
      fullName: apiSummary.fullName,
      department: apiSummary.department,
      position: apiSummary.position,
      employmentStatus: apiSummary.employmentStatus,
      hireDate: new Date(apiSummary.hireDate)
    };
  }

  /**
   * Creates a new employee creation request for API
   * 
   * Formats frontend employee data for API submission including:
   * - Date object to date string conversion
   * - Field mapping for API expectations
   * - Optional field handling
   * 
   * @static
   * @param {Object} employeeData - Frontend employee form data
   * @param {string} employeeData.employeeId - Desired employee ID/badge number
   * @param {string} employeeData.firstName - Employee's first name
   * @param {string} employeeData.lastName - Employee's last name
   * @param {string} employeeData.email - Employee's email address
   * @param {string} [employeeData.phoneNumber] - Employee's phone number
   * @param {Date} [employeeData.dateOfBirth] - Date of birth
   * @param {Date} employeeData.hireDate - Hire date
   * @param {string} employeeData.department - Department name
   * @param {string} employeeData.position - Job position
   * @param {string|number} [employeeData.managerId] - Manager's ID
   * @param {string} employeeData.employmentType - Type of employment
   * @param {string} [employeeData.workLocation] - Work location
   * @param {Object} [employeeData.address] - Employee's address
   * @param {Object} [employeeData.emergencyContact] - Emergency contact
   * @param {string} [employeeData.emergencyPhone] - Emergency phone
   * @param {string} [employeeData.bankAccountNumber] - Bank account (unmasked for API)
   * @returns {Object} Formatted request object for employee creation API
   * 
   * @example
   * const formData = {
   *   employeeId: 'EMP002',
   *   firstName: 'Jane',
   *   lastName: 'Smith',
   *   email: 'jane.smith@company.com',
   *   hireDate: new Date('2024-01-15'),
   *   department: 'Engineering',
   *   position: 'Senior Developer',
   *   employmentType: 'Full-time'
   * };
   * 
   * const request = EmployeeMapper.toCreateRequest(formData);
   * // Returns:
   * // {
   * //   employeeId: 'EMP002',
   * //   firstName: 'Jane',
   * //   lastName: 'Smith',
   * //   email: 'jane.smith@company.com',
   * //   hireDate: '2024-01-15',
   * //   department: 'Engineering',
   * //   position: 'Senior Developer',
   * //   employmentType: 'Full-time'
   * // }
   */
  static toCreateRequest(employeeData) {
    return {
      employeeId: employeeData.employeeId,
      firstName: employeeData.firstName,
      lastName: employeeData.lastName,
      email: employeeData.email,
      phoneNumber: employeeData.phoneNumber,
      dateOfBirth: employeeData.dateOfBirth?.toISOString().split('T')[0],
      hireDate: employeeData.hireDate.toISOString().split('T')[0],
      department: employeeData.department,
      position: employeeData.position,
      managerId: employeeData.managerId,
      employmentType: employeeData.employmentType,
      workLocation: employeeData.workLocation,
      address: employeeData.address,
      emergencyContact: employeeData.emergencyContact,
      emergencyPhone: employeeData.emergencyPhone,
      bankAccountNumber: employeeData.bankAccountNumber
    };
  }
}

export default EmployeeMapper;