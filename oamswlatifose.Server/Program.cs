using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using oamswlatifose.Server.Extensions;
using oamswlatifose.Server.Middleware;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Services.Email;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionAuthorization();
builder.Services.AddAutoMapperProfiles();


builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddEmailOptions(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddValidators();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddCustomResponseCaching();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLicensing();
var app = builder.Build();

// Apply migrations on every startup — runs before any request is served.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed demo accounts (idempotent — safe on every startup).
await app.SeedDevAsync();

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseMiddleware<LicenseMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile("/index.html");

app.Run();