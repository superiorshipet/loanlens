using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LoanLens.Api.Health;
using LoanLens.Api.Middleware;
using LoanLens.Application;
using LoanLens.Infrastructure;
using LoanLens.Infrastructure.Entities;
using LoanLens.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure JSON serialization options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
});

// 2. Register Application & Infrastructure Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Register Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

// 4. Configure JWT Authentication
var jwtSecret = builder.Configuration.GetValue<string>("Jwt:Secret") 
                ?? "LoanLensSuperSecretKeyForJwtSigningMustBeVeryLongAndSecure2026!";
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "LoanLens.Api";
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "LoanLens.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Register RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();

// 6. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// 7. Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
        limiterOptions.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60));
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 10);
    });
});

// 8. Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres_db", tags: ["db", "ready"])
    .AddCheck<RedisHealthCheck>("redis_cache", tags: ["cache", "ready"]);

// 9. Configure OpenAPI / Scalar
builder.Services.AddOpenApi();

var app = builder.Build();

// 10. Pipeline Middlewares
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// 11. Swagger / Scalar API Documentation & Root Redirect
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

// 12. Health Check Endpoints
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/ready");

// 13. Map API Controllers
app.MapControllers().RequireRateLimiting("fixed");

// 14. Auto-Apply Database Migrations and Seed Default Admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanLensDbContext>();
    db.Database.Migrate();

    // Seed default admin user if no users exist
    if (!db.Users.Any())
    {
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "admin@loanlens.com",
            FullName = "System Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            Role = LoanLens.Domain.Enums.UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

app.Run();
