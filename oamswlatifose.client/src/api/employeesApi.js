// src/api/employeesApi.js
import ApiService from './apiService';
import EncryptionUtil from '../utils/encryption';

/**
 * Employees API Service
 * 
 * Handles all employee-related API operations including CRUD operations,
 * searching, filtering, and data export. Implements field-level encryption
 * for sensitive employee information.
 * 
 * @class EmployeesApi
 * @extends ApiService
 */
class EmployeesApi extends ApiService {
  /**
   * Creates an instance of EmployeesApi
   * Sets the base endpoint to '/employees' for all employee-related requests
   * 
   * @constructor
   */
  constructor() {
    super('/employees');
  }

  /**
   * Retrieve paginated list of employees
   * 
   * Fetches employees with server-side pagination support.
   * 
   * @async
   * @param {number} [pageNumber=1] - Page number to retrieve (starts at 1)
   * @param {number} [pageSize=10] - Number of employees per page
   * @returns {Promise<Object>} Response containing paginated employee data
   * @property {Array} data.employees - List of employees for current page
   * @property {number} data.totalCount - Total number of employees
   * @property {number} data.pageNumber - Current page number
   * @property {number} data.pageSize - Page size used
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getEmployees(1, 20);
   * const { employees, totalCount } = response.data;
   */
  async getEmployees(pageNumber = 1, pageSize = 10) {
    return this.get('', { pageNumber, pageSize });
  }

  /**
   * Retrieve a single employee by ID
   * 
   * Fetches detailed information for a specific employee.
   * 
   * @async
   * @param {string|number} id - Unique identifier of the employee
   * @returns {Promise<Object>} Response containing employee details
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getEmployeeById(123);
   * const employee = response.data;
   */
  async getEmployeeById(id) {
    return this.get(`/${id}`);
  }

  /**
   * Retrieve employee by badge number
   * 
   * Fetches employee information using their unique badge/employee ID.
   * 
   * @async
   * @param {string|number} employeeId - Employee's badge number or ID
   * @returns {Promise<Object>} Response containing employee details
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getEmployeeByBadge('EMP001');
   */
  async getEmployeeByBadge(employeeId) {
    return this.get(`/badge/${employeeId}`);
  }

  /**
   * Create a new employee record
   * 
   * Creates a new employee with automatic encryption of sensitive fields.
   * 
   * @async
   * @param {Object} createDto - Employee creation data
   * @param {string} createDto.firstName - Employee's first name
   * @param {string} createDto.lastName - Employee's last name
   * @param {string} createDto.email - Employee's email address (encrypted)
   * @param {string} createDto.phoneNumber - Employee's phone number (encrypted)
   * @param {string} createDto.address - Employee's address (encrypted)
   * @param {Object} createDto.emergencyContact - Emergency contact info (encrypted)
   * @param {string} createDto.bankAccountNumber - Bank account for payroll (encrypted)
   * @param {string} createDto.department - Employee's department
   * @param {string} createDto.position - Employee's job position
   * @returns {Promise<Object>} Response containing created employee data
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.createEmployee({
   *   firstName: 'Jane',
   *   lastName: 'Smith',
   *   email: 'jane.smith@company.com',
   *   phoneNumber: '+1234567890',
   *   department: 'Engineering',
   *   position: 'Developer'
   * });
   */
  async createEmployee(createDto) {
    // Encrypt sensitive employee data
    const sensitiveFields = ['email', 'phoneNumber', 'address', 'emergencyContact', 'bankAccountNumber'];
    const encryptedData = EncryptionUtil.encryptSensitiveFields(createDto, sensitiveFields);
    return this.post('', encryptedData, true);
  }

  /**
   * Update an existing employee record
   * 
   * Updates employee information with automatic encryption of sensitive fields.
   * 
   * @async
   * @param {string|number} id - Unique identifier of the employee to update
   * @param {Object} updateDto - Employee update data (partial updates supported)
   * @returns {Promise<Object>} Response containing updated employee data
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.updateEmployee(123, {
   *   position: 'Senior Developer',
   *   department: 'Engineering'
   * });
   */
  async updateEmployee(id, updateDto) {
    const sensitiveFields = ['email', 'phoneNumber', 'address', 'emergencyContact', 'bankAccountNumber'];
    const encryptedData = EncryptionUtil.encryptSensitiveFields(updateDto, sensitiveFields);
    return this.put(`/${id}`, encryptedData, true);
  }

  /**
   * Delete an employee record
   * 
   * Permanently removes an employee from the system.
   * 
   * @async
   * @param {string|number} id - Unique identifier of the employee to delete
   * @returns {Promise<Object>} Response confirming deletion
   * @throws {Object} Standardized error object
   * 
   * @example
   * await employeesApi.deleteEmployee(123);
   */
  async deleteEmployee(id) {
    return this.delete(`/${id}`);
  }

  /**
   * Search employees by term
   * 
   * Searches employees across multiple fields (name, email, department, etc.)
   * 
   * @async
   * @param {string} term - Search term to look for
   * @returns {Promise<Object>} Response containing matching employees
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.searchEmployees('John');
   */
  async searchEmployees(term) {
    return this.get('/search', { term });
  }

  /**
   * Get employees by department
   * 
   * Retrieves all employees belonging to a specific department.
   * 
   * @async
   * @param {string} department - Department name
   * @returns {Promise<Object>} Response containing department employees
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getEmployeesByDepartment('Engineering');
   */
  async getEmployeesByDepartment(department) {
    return this.get(`/department/${department}`);
  }

  /**
   * Get employees by position
   * 
   * Retrieves all employees with a specific job position.
   * 
   * @async
   * @param {string} position - Job position title
   * @returns {Promise<Object>} Response containing employees with that position
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getEmployeesByPosition('Manager');
   */
  async getEmployeesByPosition(position) {
    return this.get(`/position/${position}`);
  }

  /**
   * Get employees without user accounts
   * 
   * Retrieves employees who don't have associated user accounts.
   * Useful for account creation workflows.
   * 
   * @async
   * @returns {Promise<Object>} Response containing employees without accounts
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getEmployeesWithoutAccounts();
   */
  async getEmployeesWithoutAccounts() {
    return this.get('/no-account');
  }

  /**
   * Get department statistics
   * 
   * Retrieves statistical data about employees across departments.
   * 
   * @async
   * @returns {Promise<Object>} Response containing department statistics
   * @property {Array} data.departmentStats - Statistics per department
   * @property {number} data.totalEmployees - Total employee count
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await employeesApi.getDepartmentStatistics();
   * const { departmentStats } = response.data;
   */
  async getDepartmentStatistics() {
    return this.get('/statistics/departments');
  }

  /**
   * Export employees data to file
   * 
   * Exports employee data to specified format and triggers download.
   * 
   * @async
   * @param {string} [format='excel'] - Export format ('excel', 'csv', 'pdf')
   * @returns {Promise<Blob>} File blob for download
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Export to Excel
   * await employeesApi.exportEmployees('excel');
   * 
   * // Export to CSV
   * await employeesApi.exportEmployees('csv');
   */
  async exportEmployees(format = 'excel') {
    return this.download('/export', { format }, `employees_${Date.now()}.${format}`);
  }
}

// Export a singleton instance
export default new EmployeesApi();