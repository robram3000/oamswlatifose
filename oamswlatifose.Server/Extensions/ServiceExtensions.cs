using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using oamswlatifose.Server.MappingProfiles;
using oamswlatifose.Server.Middleware;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Repository.AttendanceManagement.Implementation;
using oamswlatifose.Server.Repository.AttendanceManagement.Interfaces;
using oamswlatifose.Server.Repository.AuditManagement.Implementations;
using oamswlatifose.Server.Repository.AuditManagement.Interfaces;
using oamswlatifose.Server.Repository.EmailManagement.Implementations;
using oamswlatifose.Server.Repository.EmailManagement.Interfaces;
using oamswlatifose.Server.Repository.EmployeeManagement.Implementation;
using oamswlatifose.Server.Repository.EmployeeManagement.Interface;
using oamswlatifose.Server.Repository.RoleManagement.Implementations;
using oamswlatifose.Server.Repository.RoleManagement.Interfaces;
using oamswlatifose.Server.Repository.SessionManagement.Implementations;
using oamswlatifose.Server.Repository.SessionManagement.Interfaces;
using oamswlatifose.Server.Repository.TokenManagement.Implementations;
using oamswlatifose.Server.Repository.TokenManagement.Interfaces;
using oamswlatifose.Server.Repository.UserManagement.Implementations;
using oamswlatifose.Server.Repository.UserManagement.Interfaces;
using oamswlatifose.Server.Services.EmployeeManagement.Implementation;
using oamswlatifose.Server.Services.EmployeeManagement.Interfaces;
using oamswlatifose.Server.Utilities.Security;
using oamswlatifose.Server.Validations.Validators;
using System.Reflection;
using System.Text;

namespace oamswlatifose.Server.Extensions
{
    /// <summary>
    /// Extension methods for service registration and configuration.
    /// Provides centralized dependency injection setup.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Configures CORS policies for the application.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "https://localhost:4200" };

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("X-Pagination", "X-Correlation-ID");
                });
            });

            return services;
        }

        /// <summary>
        /// Configures JWT authentication.
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

            services.AddSingleton(jwtSettings);
            services.AddSingleton<JwtTokenGenerator>();

            var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "unique_name"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Cookies["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Configures authorization with permission-based policies.
        /// </summary>
        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddAuthorization();

            return services;
        }

        /// <summary>
        /// Configures AutoMapper with all profiles.
        /// </summary>
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<EmployeeMappingProfile>();
                cfg.AddProfile<UserMappingProfile>();
                cfg.AddProfile<AttendanceMappingProfile>();
                cfg.AddProfile<RoleMappingProfile>();
            }, typeof(Program).Assembly);

            return services;
        }

        /// <summary>
        /// Registers all repositories.
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Employee repositories
            services.AddScoped<IEmployeeManagementQueryRepository, EmployeeManagementQueryRepository>();
            services.AddScoped<IEmployeeManagementCommandRepository, EmployeeManagementCommandRepository>();

            // Attendance repositories
            services.AddScoped<IAttendanceTrackingQueryRepository, AttendanceTrackingQueryRepository>();
            services.AddScoped<IAttendanceTrackingCommandRepository, AttendanceTrackingCommandRepository>();

            // User repositories
            services.AddScoped<IUserAccountQueryRepository, UserAccountQueryRepository>();
            services.AddScoped<IUserAccountCommandRepository, UserAccountCommandRepository>();

            // Role repositories
            services.AddScoped<IRoleBasedAccessQueryRepository, RoleBasedAccessQueryRepository>();
            services.AddScoped<IRoleBasedAccessCommandRepository, RoleBasedAccessCommandRepository>();

            // Token repositories
            services.AddScoped<IJwtTokenManagementQueryRepository, JwtTokenManagementQueryRepository>();
            services.AddScoped<IJwtTokenManagementCommandRepository, JwtTokenManagementCommandRepository>();

            // Session repositories
            services.AddScoped<ISessionManagementQueryRepository, SessionManagementQueryRepository>();
            services.AddScoped<ISessionManagementCommandRepository, SessionManagementCommandRepository>();

            // Audit repositories
            services.AddScoped<IAuthenticationAuditQueryRepository, AuthenticationAuditQueryRepository>();
            services.AddScoped<IAuthenticationAuditCommandRepository, AuthenticationAuditCommandRepository>();

            // Email log repositories
            services.AddScoped<IEmailNotificationLogQueryRepository, EmailNotificationLogQueryRepository>();
            services.AddScoped<IEmailNotificationLogCommandRepository, EmailNotificationLogCommandRepository>();

            return services;
        }

        /// <summary>
        /// Registers all services.
        /// </summary>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Add correlation ID generator
            services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();

            return services;
        }

        /// <summary>
        /// Registers all validators.
        /// </summary>
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<CreateEmployeeValidator>();

            return services;
        }

        /// <summary>
        /// Configures Swagger/OpenAPI.
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Employee Management System API",
                    Version = "v1",
                    Description = "Comprehensive API for employee management, attendance tracking, and authentication",
                    Contact = new OpenApiContact
                    {
                        Name = "Support Team",
                        Email = "support@ems.com"
                    }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// Configures health checks.
        /// </summary>
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>()
                .AddUrlGroup(new Uri(configuration["ExternalServices:EmailService"] ?? "http://localhost:5000"),
                    "Email Service")
                .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 500)
                .AddDiskStorageHealthCheck(configure =>
                    configure.AddDrive("C:\\", minimumFreeMegabytes: 10240));

            return services;
        }

        /// <summary>
        /// Configures response caching.
        /// </summary>
        public static IServiceCollection AddResponseCaching(this IServiceCollection services)
        {
            services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
                options.SizeLimit = 100 * 1024 * 1024; // 100 MB
                options.UseCaseSensitivePaths = false;
            });

            return services;
        }
    }
}
