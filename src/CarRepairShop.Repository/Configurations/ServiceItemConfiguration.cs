using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("ServiceItems");

        builder.HasKey(si => si.Id);

        builder.Property(si => si.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(si => si.Description)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(si => si.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(si => si.Stock)
            .IsRequired();

        builder.Property(si => si.CreatedAt)
            .IsRequired();

        builder.Property(si => si.CreatedUserId);

        builder.Property(si => si.LastUpdatedUserId);
    }
}
