/**
 * Attendance Data Mapper
 * 
 * This mapper class transforms attendance data between API response formats,
 * frontend model formats, and various presentation formats (charts, summaries).
 * It handles date conversions, data formatting, and prepares data for different
 * UI components.
 * 
 * @class AttendanceMapper
 * @static
 * 
 * @example
 * // Convert API response to frontend attendance model
 * const attendance = AttendanceMapper.toAttendance(apiResponse.data);
 * 
 * // Prepare chart data for visualization
 * const chartData = AttendanceMapper.toChartData(attendanceRecords);
 */
class AttendanceMapper {
  /**
   * Maps raw API attendance data to frontend model
   * 
   * Transforms API response format into a more usable frontend format with:
   * - Date string to Date object conversions
   * - Null handling for optional fields
   * - Formatted time strings preserved
   * - Calculated fields (hoursWorked, overtimeHours) kept as-is
   * 
   * @static
   * @param {Object} apiAttendance - Raw attendance data from API
   * @param {string|number} apiAttendance.id - Attendance record ID
   * @param {string|number} apiAttendance.employeeId - Employee ID
   * @param {string} apiAttendance.employeeName - Employee full name
   * @param {string} apiAttendance.date - Date string (ISO format)
   * @param {string} [apiAttendance.timeIn] - Time in ISO string
   * @param {string} [apiAttendance.timeOut] - Time out ISO string
   * @param {string} [apiAttendance.timeInFormatted] - Formatted time in (e.g., "09:00 AM")
   * @param {string} [apiAttendance.timeOutFormatted] - Formatted time out
   * @param {number} apiAttendance.hoursWorked - Total hours worked
   * @param {number} apiAttendance.overtimeHours - Overtime hours
   * @param {string} apiAttendance.status - Attendance status (Present/Absent/Late/etc)
   * @param {boolean} apiAttendance.isLate - Whether employee was late
   * @param {number} apiAttendance.lateMinutes - Minutes late
   * @param {boolean} apiAttendance.isOvertime - Whether employee worked overtime
   * @param {string} [apiAttendance.notes] - Additional notes
   * @param {Object} [apiAttendance.location] - Geolocation data
   * @param {string} [apiAttendance.ipAddress] - IP address of clock-in device
   * @param {Object} [apiAttendance.deviceInfo] - Device information
   * @returns {Object} Formatted attendance object for frontend use
   * 
   * @example
   * const apiResponse = {
   *   id: 123,
   *   employeeId: 456,
   *   employeeName: 'John Doe',
   *   date: '2024-01-15',
   *   timeIn: '2024-01-15T09:00:00Z',
   *   timeOut: '2024-01-15T17:00:00Z',
   *   timeInFormatted: '09:00 AM',
   *   timeOutFormatted: '05:00 PM',
   *   hoursWorked: 8,
   *   overtimeHours: 0,
   *   status: 'Present',
   *   isLate: false,
   *   lateMinutes: 0,
   *   isOvertime: false
   * };
   * 
   * const attendance = AttendanceMapper.toAttendance(apiResponse);
   * // Returns:
   * // {
   * //   id: 123,
   * //   employeeId: 456,
   * //   employeeName: 'John Doe',
   * //   date: Date object,
   * //   timeIn: Date object,
   * //   timeOut: Date object,
   * //   timeInFormatted: '09:00 AM',
   * //   ...
   * // }
   */
  static toAttendance(apiAttendance) {
    return {
      id: apiAttendance.id,
      employeeId: apiAttendance.employeeId,
      employeeName: apiAttendance.employeeName,
      date: new Date(apiAttendance.date),
      timeIn: apiAttendance.timeIn ? new Date(apiAttendance.timeIn) : null,
      timeOut: apiAttendance.timeOut ? new Date(apiAttendance.timeOut) : null,
      timeInFormatted: apiAttendance.timeInFormatted,
      timeOutFormatted: apiAttendance.timeOutFormatted,
      hoursWorked: apiAttendance.hoursWorked,
      overtimeHours: apiAttendance.overtimeHours,
      status: apiAttendance.status,
      isLate: apiAttendance.isLate,
      lateMinutes: apiAttendance.lateMinutes,
      isOvertime: apiAttendance.isOvertime,
      notes: apiAttendance.notes,
      location: apiAttendance.location,
      ipAddress: apiAttendance.ipAddress,
      deviceInfo: apiAttendance.deviceInfo
    };
  }

  /**
   * Creates a clock-in request object for API
   * 
   * Formats employee clock-in data for API submission including
   * timestamp generation and optional location/notes.
   * 
   * @static
   * @param {string|number} employeeId - ID of employee clocking in
   * @param {Object} [location=null] - Geolocation coordinates
   * @param {number} location.latitude - Latitude coordinate
   * @param {number} location.longitude - Longitude coordinate
   * @param {string} [notes=null] - Optional clock-in notes
   * @returns {Object} Formatted request object for clock-in API
   * 
   * @example
   * // Simple clock-in
   * const request = AttendanceMapper.toClockInRequest(456);
   * 
   * // Clock-in with location
   * const requestWithLocation = AttendanceMapper.toClockInRequest(
   *   456,
   *   { latitude: 40.7128, longitude: -74.0060 }
   * );
   * 
   * // Full clock-in with notes
   * const fullRequest = AttendanceMapper.toClockInRequest(
   *   456,
   *   { latitude: 40.7128, longitude: -74.0060 },
   *   'Working from office'
   * );
   */
  static toClockInRequest(employeeId, location = null, notes = null) {
    return {
      employeeId,
      location,
      notes,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Creates a clock-out request object for API
   * 
   * Formats employee clock-out data for API submission including
   * timestamp generation and optional location/notes.
   * 
   * @static
   * @param {string|number} employeeId - ID of employee clocking out
   * @param {Object} [location=null] - Geolocation coordinates
   * @param {number} location.latitude - Latitude coordinate
   * @param {number} location.longitude - Longitude coordinate
   * @param {string} [notes=null] - Optional clock-out notes
   * @returns {Object} Formatted request object for clock-out API
   * 
   * @example
   * const request = AttendanceMapper.toClockOutRequest(
   *   456,
   *   { latitude: 40.7128, longitude: -74.0060 },
   *   'Leaving for the day'
   * );
   */
  static toClockOutRequest(employeeId, location = null, notes = null) {
    return {
      employeeId,
      location,
      notes,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Maps attendance summary data from API to frontend format
   * 
   * Transforms summary statistics into a more usable format with
   * date string to Date object conversions.
   * 
   * @static
   * @param {Object} apiSummary - Summary data from API
   * @param {number} apiSummary.totalDays - Total working days in period
   * @param {number} apiSummary.presentDays - Days present
   * @param {number} apiSummary.absentDays - Days absent
   * @param {number} apiSummary.lateDays - Days late
   * @param {number} apiSummary.totalHours - Total hours worked
   * @param {number} apiSummary.averageHours - Average hours per day
   * @param {number} apiSummary.overtimeHours - Total overtime hours
   * @param {number} apiSummary.attendanceRate - Attendance percentage
   * @param {string} apiSummary.startDate - Period start date
   * @param {string} apiSummary.endDate - Period end date
   * @returns {Object} Formatted summary object
   * 
   * @example
   * const summary = AttendanceMapper.toSummary({
   *   totalDays: 22,
   *   presentDays: 20,
   *   absentDays: 2,
   *   lateDays: 1,
   *   totalHours: 160,
   *   averageHours: 8,
   *   overtimeHours: 5,
   *   attendanceRate: 90.9,
   *   startDate: '2024-01-01',
   *   endDate: '2024-01-31'
   * });
   */
  static toSummary(apiSummary) {
    return {
      totalDays: apiSummary.totalDays,
      presentDays: apiSummary.presentDays,
      absentDays: apiSummary.absentDays,
      lateDays: apiSummary.lateDays,
      totalHours: apiSummary.totalHours,
      averageHours: apiSummary.averageHours,
      overtimeHours: apiSummary.overtimeHours,
      attendanceRate: apiSummary.attendanceRate,
      startDate: new Date(apiSummary.startDate),
      endDate: new Date(apiSummary.endDate)
    };
  }

  /**
   * Converts attendance records to chart-compatible data format
   * 
   * Transforms a collection of attendance records into a format
   * suitable for visualization libraries (Chart.js, Recharts, etc.)
   * Creates two datasets: presence indicators and hours worked.
   * 
   * @static
   * @param {Array<Object>} attendanceRecords - Array of attendance records
   * @param {Date} attendanceRecords[].date - Attendance date
   * @param {number} attendanceRecords[].hoursWorked - Hours worked that day
   * @returns {Object} Chart.js compatible data object
   * @property {Array<string>} labels - Date strings for x-axis
   * @property {Array<Object>} datasets - Array of dataset configurations
   * 
   * @example
   * const records = [
   *   { date: new Date('2024-01-15'), hoursWorked: 8 },
   *   { date: new Date('2024-01-16'), hoursWorked: 7.5 }
   * ];
   * 
   * const chartData = AttendanceMapper.toChartData(records);
   * // Returns:
   * // {
   * //   labels: ['1/15/2024', '1/16/2024'],
   * //   datasets: [
   * //     {
   * //       label: 'Present',
   * //       data: [1, 1],
   * //       backgroundColor: 'rgba(75, 192, 192, 0.6)'
   * //     },
   * //     {
   * //       label: 'Hours Worked',
   * //       data: [8, 7.5],
   * //       backgroundColor: 'rgba(153, 102, 255, 0.6)'
   * //     }
   * //   ]
   * // }
   */
  static toChartData(attendanceRecords) {
    const labels = [];
    const presentData = [];
    const hoursData = [];

    attendanceRecords.forEach(record => {
      labels.push(record.date.toLocaleDateString());
      presentData.push(record.hoursWorked > 0 ? 1 : 0);
      hoursData.push(record.hoursWorked);
    });

    return {
      labels,
      datasets: [
        {
          label: 'Present',
          data: presentData,
          backgroundColor: 'rgba(75, 192, 192, 0.6)'
        },
        {
          label: 'Hours Worked',
          data: hoursData,
          backgroundColor: 'rgba(153, 102, 255, 0.6)'
        }
      ]
    };
  }
}

export default AttendanceMapper;