using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceOrderJobStatusHistoryConfiguration : IEntityTypeConfiguration<ServiceOrderJobStatusHistory>
{
    public void Configure(EntityTypeBuilder<ServiceOrderJobStatusHistory> builder)
    {
        builder.ToTable("ServiceOrderJobStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ServiceOrderJobId)
            .IsRequired();

        builder.Property(h => h.FromStatus);

        builder.Property(h => h.ToStatus)
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId);

        builder.HasIndex(h => h.ServiceOrderJobId);
    }
}
