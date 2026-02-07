using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using oamswlatifose.Server.Repository.Rsmtp;
using oamswlatifose.Server.Smtp;

namespace oamswlatifose.Server.Services.Email
{

    public static class SmtpServiceExtensions
    {
        public static IServiceCollection AddSmtpServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "SmtpSettings")
        {
            // Add AutoMapper with the specific assembly containing profiles
            services.AddAutoMapper(cfg => { }, typeof(EmailMapperProfile).Assembly);

            // Configure from appsettings.json
            services.Configure<SmtpConfiguration>(configuration.GetSection(sectionName));
            services.Configure<DefaultSenderEmail>(configuration.GetSection("EmailSettings:Sender"));

            // Register configurations
            services.AddSingleton<SmtpConfiguration>(provider =>
            {
                var config = new SmtpConfiguration();
                configuration.GetSection(sectionName).Bind(config);
                return config;
            });

            services.AddSingleton<DefaultSenderEmail>(provider =>
            {
                var sender = new DefaultSenderEmail();
                configuration.GetSection("EmailSettings:Sender").Bind(sender);
                return sender;
            });

            // Register services
            services.AddSingleton<TemplateOTPVerification>();
            services.AddSingleton<IEmailService, EmailService>();

            // Register logging repository
            services.AddSingleton<IEmailLogRepository, InMemoryEmailLogRepository>();

            return services;
        }

        public static IServiceCollection AddSmtpServicesWithMapper(
            this IServiceCollection services,
            Action<SmtpConfiguration> configureSmtp,
            Action<DefaultSenderEmail> configureSender = null)
        {
            // Add AutoMapper with the assembly containing EmailMapperProfile
            services.AddAutoMapper(cfg => { }, typeof(EmailMapperProfile).Assembly);

            
            // Configure SMTP
            var smtpConfig = new SmtpConfiguration();
            configureSmtp?.Invoke(smtpConfig);
            services.AddSingleton(smtpConfig);

            // Configure Sender
            var senderEmail = new DefaultSenderEmail();
            configureSender?.Invoke(senderEmail);
            services.AddSingleton(senderEmail);

            // Register services
            services.AddSingleton<TemplateOTPVerification>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<IEmailLogRepository, InMemoryEmailLogRepository>();

            return services;
        }
    }
}
