using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceJobStatusHistoryConfiguration : IEntityTypeConfiguration<ServiceJobStatusHistory>
{
    public void Configure(EntityTypeBuilder<ServiceJobStatusHistory> builder)
    {
        builder.ToTable("ServiceJobStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ServiceJobId)
            .IsRequired();

        builder.Property(h => h.FromStatus);

        builder.Property(h => h.ToStatus)
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId);

        builder.HasIndex(h => h.ServiceJobId);
    }
}
