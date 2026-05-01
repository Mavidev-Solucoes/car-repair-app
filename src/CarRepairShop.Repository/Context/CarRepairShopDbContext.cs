using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.Repository.Context;

public class CarRepairShopDbContext : DbContext
{
    public CarRepairShopDbContext(DbContextOptions<CarRepairShopDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ServiceOrder> ServiceOrders { get; set; }
    public DbSet<ServiceOrderItem> ServiceOrderItems { get; set; }
    public DbSet<ServiceItem> ServiceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceOrderConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceOrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceItemConfiguration());
    }
}
