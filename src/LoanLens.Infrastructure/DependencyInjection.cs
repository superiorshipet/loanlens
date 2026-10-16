using LoanLens.Application.Interfaces;
using LoanLens.Infrastructure.Persistence;
using LoanLens.Infrastructure.Repositories;
using LoanLens.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? "Host=localhost;Port=5432;Database=loanlens_db;Username=superior;Password=postgres;";

        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";

        if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || 
            connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<LoanLensDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
        }
        else
        {
            services.AddDbContext<LoanLensDbContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
        }

        services.Configure<Configuration.AiAdvisorOptions>(
            configuration.GetSection(Configuration.AiAdvisorOptions.SectionName));

        services.Configure<Configuration.JwtOptions>(
            configuration.GetSection(Configuration.JwtOptions.SectionName));

        services.Configure<Configuration.RedisOptions>(
            configuration.GetSection(Configuration.RedisOptions.SectionName));

        services.AddMemoryCache();

        var redisOptions = configuration.GetSection(Configuration.RedisOptions.SectionName).Get<Configuration.RedisOptions>()
                           ?? new Configuration.RedisOptions();

        if (redisOptions.Enabled && !string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstanceName;
            });
        }

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IPdfReportService, LoanPdfReportService>();
        services.AddScoped<IExcelExportService, LoanExcelExportService>();
        services.AddHttpClient<ILoanNarrativeAdvisor, LoanNarrativeAdvisorService>();

        return services;
    }
}
