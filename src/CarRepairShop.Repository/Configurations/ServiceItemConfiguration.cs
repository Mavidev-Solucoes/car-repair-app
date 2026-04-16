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

        builder.Property(si => si.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(si => si.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(si => si.Quantity)
            .IsRequired();

        builder.Property(si => si.CreatedAt)
            .IsRequired();
    }
}
