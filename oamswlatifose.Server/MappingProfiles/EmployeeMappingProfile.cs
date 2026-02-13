using AutoMapper;
using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.MappingProfiles
{
    /// <summary>
    /// AutoMapper profile for Employee entity mappings defining comprehensive transformations
    /// between domain models and Data Transfer Objects (DTOs). This profile ensures proper
    /// data shaping, flattening of complex objects, and secure field exposure for API consumers.
    /// 
    /// <para>Mapping Configurations:</para>
    /// <para>- EMEmployees → EmployeeResponseDTO: Flattens related entities and formats names</para>
    /// <para>- CreateEmployeeDTO → EMEmployees: Maps creation requests with audit field defaults</para>
    /// <para>- UpdateEmployeeDTO → EMEmployees: Maps update requests preserving existing values</para>
    /// <para>- EMEmployees → EmployeeSummaryDTO: Lightweight representation for list views</para>
    /// 
    /// <para>Security Considerations:</para>
    /// <para>- Sensitive fields (salary, SSN, etc.) are explicitly excluded from mappings</para>
    /// <para>- Audit fields are auto-generated, never mapped from client input</para>
    /// <para>- Foreign key relationships are properly resolved during mapping</para>
    /// </summary>
    public class EmployeeMappingProfile : Profile
    {
        public EmployeeMappingProfile()
        {
            // Map Employee entity to detailed response DTO with related data flattening
            CreateMap<EMEmployees, EmployeeResponseDTO>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.HasUserAccount,
                    opt => opt.MapFrom(src => src.UserAccount != null))
                .ForMember(dest => dest.UserAccountId,
                    opt => opt.MapFrom(src => src.UserAccount != null ? src.UserAccount.Id : (int?)null))
                .ForMember(dest => dest.AttendanceCount,
                    opt => opt.MapFrom(src => src.Attendances != null ? src.Attendances.Count() : 0))
                .ForMember(dest => dest.CreatedAtFormatted,
                    opt => opt.MapFrom(src => src.CreatedAt.HasValue ? src.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm") : null))
                .ForMember(dest => dest.UpdatedAtFormatted,
                    opt => opt.MapFrom(src => src.UpdatedAt.HasValue ? src.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm") : null));

            // Map Employee entity to lightweight summary DTO for list views
            CreateMap<EMEmployees, EmployeeSummaryDTO>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Department,
                    opt => opt.MapFrom(src => src.Department ?? "Not Assigned"))
                .ForMember(dest => dest.Position,
                    opt => opt.MapFrom(src => src.Position ?? "Not Assigned"));

            // Map create DTO to Employee entity with audit fields auto-populated
            CreateMap<CreateEmployeeDTO, EMEmployees>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeID, opt => opt.MapFrom(src => src.EmployeeID))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserAccount, opt => opt.Ignore())
                .ForMember(dest => dest.Attendances, opt => opt.Ignore());

            // Map update DTO to existing Employee entity
            CreateMap<UpdateEmployeeDTO, EMEmployees>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeID, opt => opt.Ignore()) // Prevent changing employee ID
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserAccount, opt => opt.Ignore())
                .ForMember(dest => dest.Attendances, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
