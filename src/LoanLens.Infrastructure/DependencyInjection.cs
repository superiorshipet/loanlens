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
                               ?? "Data Source=loanlens.db";

        services.AddDbContext<LoanLensDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IPdfReportService, LoanPdfReportService>();
        services.AddScoped<IExcelExportService, LoanExcelExportService>();

        return services;
    }
}
