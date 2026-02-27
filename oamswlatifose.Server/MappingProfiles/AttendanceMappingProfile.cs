using AutoMapper;
using oamswlatifose.Server.DTO.attendances;
using oamswlatifose.Server.Model.occurance;

namespace oamswlatifose.Server.MappingProfiles
{
    /// <summary>
    /// AutoMapper profile for Attendance entity mappings with comprehensive
    /// time tracking, status determination, and employee relationship flattening.
    /// Handles complex calculations and formatting for attendance data presentation.
    /// 
    /// <para>Mapping Features:</para>
    /// <para>- Automatic calculation of hours worked and overtime</para>
    /// <para>- Formatting of time values for display</para>
    /// <para>- Employee name and department flattening</para>
    /// <para>- Status determination based on time-in values</para>
    /// <para>- Shift-based attendance categorization</para>
    /// </summary>
    public class AttendanceMappingProfile : Profile
    {
        public AttendanceMappingProfile()
        {
            // Map Attendance entity to detailed response DTO with calculated fields
            CreateMap<EMAttendance, AttendanceResponseDTO>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee != null
                        ? $"{src.Employee.FirstName} {src.Employee.LastName}"
                        : "Unknown"))
                .ForMember(dest => dest.Department,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Department : null))
                .ForMember(dest => dest.TimeInFormatted,
                    opt => opt.MapFrom(src => src.TimeIn.HasValue
                        ? DateTime.Today.Add(src.TimeIn.Value).ToString("hh:mm tt")
                        : null))
                .ForMember(dest => dest.TimeOutFormatted,
                    opt => opt.MapFrom(src => src.TimeOut.HasValue
                        ? DateTime.Today.Add(src.TimeOut.Value).ToString("hh:mm tt")
                        : null))
                .ForMember(dest => dest.DateFormatted,
                    opt => opt.MapFrom(src => src.AttendanceDate.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.DayOfWeek,
                    opt => opt.MapFrom(src => src.AttendanceDate.ToString("dddd")))
                .ForMember(dest => dest.HoursWorkedFormatted,
                    opt => opt.MapFrom(src => FormatHoursWorked(src.HoursWorked)))
                .ForMember(dest => dest.OvertimeFormatted,
                    opt => opt.MapFrom(src => FormatOvertime(src.OvertimeHours)))
                .ForMember(dest => dest.AttendanceStatus,
                    opt => opt.MapFrom(src => DetermineDetailedStatus(src)))
                .ForMember(dest => dest.StatusColor,
                    opt => opt.MapFrom(src => GetStatusColor(src.Status)))
                .ForMember(dest => dest.IsComplete,
                    opt => opt.MapFrom(src => src.TimeIn.HasValue && src.TimeOut.HasValue))
                .ForMember(dest => dest.CreatedAtFormatted,
                    opt => opt.MapFrom(src => src.CreatedAt.HasValue
                        ? src.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm")
                        : null));

            // Map Attendance entity to summary DTO for list views
            CreateMap<EMAttendance, AttendanceSummaryDTO>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee != null
                        ? $"{src.Employee.FirstName} {src.Employee.LastName}"
                        : "Unknown"))
                .ForMember(dest => dest.Date,
                    opt => opt.MapFrom(src => src.AttendanceDate.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.TimeIn,
                    opt => opt.MapFrom(src => src.TimeIn.HasValue
                        ? DateTime.Today.Add(src.TimeIn.Value).ToString("hh:mm tt")
                        : "--:--"))
                .ForMember(dest => dest.TimeOut,
                    opt => opt.MapFrom(src => src.TimeOut.HasValue
                        ? DateTime.Today.Add(src.TimeOut.Value).ToString("hh:mm tt")
                        : "--:--"))
                .ForMember(dest => dest.HoursWorked,
                    opt => opt.MapFrom(src => src.HoursWorked.HasValue
                        ? src.HoursWorked.Value.ToString("F1") + "h"
                        : "0h"));

            // Map create attendance request to Attendance entity
            CreateMap<CreateAttendanceDTO, EMAttendance>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.HoursWorked, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Present"));

            // Map update attendance request to Attendance entity
            CreateMap<UpdateAttendanceDTO, EMAttendance>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.AttendanceDate, opt => opt.Ignore())
                .ForMember(dest => dest.HoursWorked, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Map clock-in request to Attendance entity
            CreateMap<ClockInDTO, EMAttendance>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AttendanceDate, opt => opt.MapFrom(src => DateTime.UtcNow.Date))
                .ForMember(dest => dest.TimeIn, opt => opt.MapFrom(src => src.TimeIn ?? DateTime.UtcNow.TimeOfDay))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Present"))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

            // Map clock-out request to update existing Attendance
            CreateMap<ClockOutDTO, EMAttendance>()
                .ForMember(dest => dest.TimeOut, opt => opt.MapFrom(src => src.TimeOut ?? DateTime.UtcNow.TimeOfDay))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }

        private string FormatHoursWorked(decimal? hours)
        {
            if (!hours.HasValue) return "0h";

            var totalHours = (int)hours.Value;
            var minutes = (int)((hours.Value - totalHours) * 60);

            return minutes > 0
                ? $"{totalHours}h {minutes}m"
                : $"{totalHours}h";
        }

        private string FormatOvertime(decimal? overtime)
        {
            if (!overtime.HasValue || overtime.Value <= 0) return null;

            var totalHours = (int)overtime.Value;
            var minutes = (int)((overtime.Value - totalHours) * 60);

            return minutes > 0
                ? $"{totalHours}h {minutes}m"
                : $"{totalHours}h";
        }

        private string DetermineDetailedStatus(EMAttendance attendance)
        {
            if (attendance.Status == "Absent") return "Absent";
            if (!attendance.TimeIn.HasValue) return "Not Checked In";
            if (!attendance.TimeOut.HasValue) return "Checked In";

            var shiftStart = new TimeSpan(9, 0, 0);
            var lateThreshold = new TimeSpan(9, 15, 0);

            if (attendance.TimeIn <= shiftStart) return "On Time";
            if (attendance.TimeIn <= lateThreshold) return "Late";
            return "Late - Excessive";
        }

        private string GetStatusColor(string status)
        {
            return status?.ToLower() switch
            {
                "present" => "green",
                "late" => "orange",
                "absent" => "red",
                "half-day" => "blue",
                "pending" => "gray",
                _ => "default"
            };
        }
    }
}
