using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace oamswlatifose.Server.Services
{
    public static class ServicesRegistration
    {
        /// <summary>
        /// Register framework and third-party services into the provided WebApplicationBuilder.
        /// Call from Program.cs after creating the builder: ServicesRegistration.RegisterServices(builder);
        /// </summary>
        /// <param name="builder">The application's WebApplicationBuilder.</param>
        public static void RegisterServices(WebApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddControllers();
            services.AddScoped<Session.UserDataSession>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }
    }
}
