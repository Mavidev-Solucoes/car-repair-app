using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceStatusHistoryConfiguration : IEntityTypeConfiguration<ServiceStatusHistory>
{
    public void Configure(EntityTypeBuilder<ServiceStatusHistory> builder)
    {
        builder.ToTable("ServiceStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ServiceOrderId)
            .IsRequired();

        builder.Property(h => h.FromStatus);

        builder.Property(h => h.ToStatus)
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId);

        builder.HasIndex(h => h.ServiceOrderId);
    }
}
