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

        builder.Property(so => so.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(so => so.Status)
            .IsRequired();

        builder.Property(so => so.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(so => so.Notes)
            .HasMaxLength(1000);

        builder.Property(so => so.CreatedAt)
            .IsRequired();

        builder.Property(so => so.CreatedUserId);

        builder.Property(so => so.LastUpdatedUserId);

        builder.HasMany(so => so.ServiceItems)
            .WithOne(si => si.ServiceOrder)
            .HasForeignKey(si => si.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
