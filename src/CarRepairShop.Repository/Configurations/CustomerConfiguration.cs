using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasBaseType<User>();

        builder.Property(c => c.PersonalId)
            .IsRequired()
            .HasMaxLength(14);

        builder.HasIndex(c => c.PersonalId)
            .IsUnique();

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.Telephone)
            .IsRequired()
            .HasMaxLength(11);

        builder.HasMany(c => c.Vehicles)
            .WithOne(v => v.Customer)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
