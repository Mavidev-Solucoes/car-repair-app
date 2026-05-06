using System.Diagnostics.CodeAnalysis;
using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CarRepairShop.Repository;

[ExcludeFromCodeCoverage]
public class CarRepairShopDbContextFactory : IDesignTimeDbContextFactory<CarRepairShopDbContext>
{
    public CarRepairShopDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "CarRepairShop.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CarRepairShopDbContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Use dotnet user-secrets or environment variables to configure it.");

        optionsBuilder.UseSqlServer(connectionString);

        return new CarRepairShopDbContext(optionsBuilder.Options);
    }
}
