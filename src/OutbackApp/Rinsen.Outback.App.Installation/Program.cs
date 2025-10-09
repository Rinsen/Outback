using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rinsen.DatabaseInstaller;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;

namespace Rinsen.Outback.App.Installation;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = InstallerHost.CreateBuilder();

        builder.AddServices(services =>
        {
            services.AddDbContextFactory<OutbackDbContext>((serviceProvider, options) =>
            {
                var installerOptions = serviceProvider.GetRequiredService<InstallerOptions>();
                var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(installerOptions.ConnectionString)
                {
                    InitialCatalog = installerOptions.DatabaseName,
                };
                options.UseSqlServer(connectionStringBuilder.ConnectionString);
            });

            services.AddScoped<DefaultInstaller>();
            services.AddSingleton<RandomStringGenerator>();
        });

        builder.AddDatabaseSetup<InstallerStartup>();

        builder.AddDataSeed<OutbackDataSeed>();

        await builder.Start();
    }
}

public class InstallerStartup : IDatabaseSetup
{
    public void DatabaseVersionsToInstall(List<DatabaseVersion> databaseVersions, IConfiguration configuration)
    {
        databaseVersions.Add(new InitializeDatabase(configuration));
        databaseVersions.Add(new CreateTables());
        databaseVersions.Add(new OutbackTableInstallation());
    }
}

public class OutbackDataSeed : IDataSeed
{
    private DefaultInstaller _defaultInstaller;
    private readonly ILogger<OutbackDataSeed> _logger;

    public OutbackDataSeed(DefaultInstaller defaultInstaller,
        ILogger<OutbackDataSeed> logger)
    {
        _defaultInstaller = defaultInstaller;
        _logger = logger;
    }
    
    public async Task SeedData()
    {
        
        var credentials = await _defaultInstaller.Install();

        _logger.LogInformation("Default clients installed. ClientId: {ClientId}, ClientSecret: {ClientSecret}", credentials.ClientId, credentials.Secret);
    }
}
