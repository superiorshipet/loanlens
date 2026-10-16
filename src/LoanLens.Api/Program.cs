using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LoanLens.Api.Health;
using LoanLens.Api.Middleware;
using LoanLens.Application;
using LoanLens.Infrastructure;
using LoanLens.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

// 4. Register RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();

// 5. Configure CORS
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

// 6. Configure Rate Limiting
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

// 7. Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("sqlite_db", tags: ["db", "ready"]);

// 8. Configure OpenAPI / Scalar
builder.Services.AddOpenApi();

var app = builder.Build();

// 9. Pipeline Middlewares
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

// 10. Swagger / Scalar API Documentation & Root Redirect
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

// 11. Health Check Endpoints
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/ready");

// 12. Map API Controllers
app.MapControllers().RequireRateLimiting("fixed");

// 13. Auto-Apply Database Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanLensDbContext>();
    db.Database.Migrate();
}

app.Run();
