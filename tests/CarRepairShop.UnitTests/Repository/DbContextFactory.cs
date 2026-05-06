using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Repository.Context;
using CarRepairShop.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.UnitTests.Repository;

/// <summary>Shared helpers for repository unit tests using EF Core InMemory.</summary>
internal static class DbContextFactory
{
    internal static CarRepairShopDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<CarRepairShopDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new CarRepairShopDbContext(options);
    }

    internal static Employee MakeEmployee(string name = "Bob", UserRole role = UserRole.Mechanic) =>
        new(name, $"{name.ToLower()}@example.com", "hash", role);

    internal static Customer MakeCustomer(string name = "Alice") =>
        new(name, "12345678901", $"{name.ToLower()}@example.com", "11987654321", "hash");

    internal static Vehicle MakeVehicle(Guid customerId, string plate = "ABC1234") =>
        new(customerId, "Ford", "Focus", 2022, plate, "Blue");

    internal static ServiceItem MakeServiceItem(string name = "Brake Pads") =>
        new(name, "Desc", 50m, 10);

    internal static ServiceJob MakeServiceJob(string name = "Oil Change") =>
        new(name, "Desc", 100m);
}
