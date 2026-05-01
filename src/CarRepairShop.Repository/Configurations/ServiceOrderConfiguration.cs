using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");

        builder.HasKey(so => so.Id);

        builder.Property(so => so.VehicleId)
            .IsRequired();

        builder.Property(so => so.CustomerId)
            .IsRequired();

        builder.Property(so => so.AssignedUserId)
            .IsRequired();

        builder.Property(so => so.Status)
            .IsRequired();

        builder.Property(so => so.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(so => so.CreatedAt)
            .IsRequired();

        builder.Property(so => so.UpdatedAt);

        builder.Property(so => so.CreatedUserId);

        builder.Property(so => so.LastUpdatedUserId);

        builder.HasOne(so => so.Customer)
            .WithMany()
            .HasForeignKey(so => so.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(so => so.AssignedUser)
            .WithMany()
            .HasForeignKey(so => so.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(so => so.ServiceItems)
            .WithOne(si => si.ServiceOrder)
            .HasForeignKey(si => si.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(so => so.ServiceJobs)
            .WithOne(sj => sj.ServiceOrder)
            .HasForeignKey(sj => sj.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(so => so.StatusHistory)
            .WithOne(h => h.ServiceOrder)
            .HasForeignKey(h => h.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
