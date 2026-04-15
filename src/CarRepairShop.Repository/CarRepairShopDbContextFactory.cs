using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CarRepairShop.Repository;

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
            ?? "Server=localhost;Database=CarRepairShopDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new CarRepairShopDbContext(optionsBuilder.Options);
    }
}
