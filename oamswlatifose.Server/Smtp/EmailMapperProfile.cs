
using AutoMapper;
using System.Net.Mail;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace oamswlatifose.Server.Smtp
{
    public class EmailMapperProfile : Profile
    {
        public EmailMapperProfile()
        {
            // Request to Message mappings
            CreateMap<SendEmailRequest, EmailMessage>()
                .ForMember(dest => dest.HtmlBody,
                    opt => opt.MapFrom(src => src.IsHtml ? src.Body : null))
                .ForMember(dest => dest.PlainTextBody,
                    opt => opt.MapFrom(src => !src.IsHtml ? src.Body : null))
                .ForMember(dest => dest.IsHtml,
                    opt => opt.MapFrom(src => src.IsHtml));

            // Configuration mappings
            CreateMap<EmailConfigurationDTO, SmtpConfiguration>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.SenderEmail))
                .ForMember(dest => dest.Password,
                    opt => opt.Ignore());

            CreateMap<EmailConfigurationDTO, DefaultSenderEmail>()
                .ForMember(dest => dest.EmailAddress,
                    opt => opt.MapFrom(src => src.SenderEmail))
                .ForMember(dest => dest.DisplayName,
                    opt => opt.MapFrom(src => src.SenderName));

            // Reverse mappings
            CreateMap<DefaultSenderEmail, EmailConfigurationDTO>()
                .ForMember(dest => dest.SenderEmail,
                    opt => opt.MapFrom(src => src.EmailAddress))
                .ForMember(dest => dest.SenderName,
                    opt => opt.MapFrom(src => src.DisplayName));

            // MailMessage mapping (for logging/tracking)
            CreateMap<EmailMessage, EmailLog>()
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(_ => "Pending"));
        }
    }
    public class EmailLog
    {
        public int Id { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public bool IsHtml { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string MessageId { get; set; }
    }
}
