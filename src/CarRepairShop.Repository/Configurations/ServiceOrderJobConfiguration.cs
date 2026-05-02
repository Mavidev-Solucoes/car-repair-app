using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceOrderJobConfiguration : IEntityTypeConfiguration<ServiceOrderJob>
{
    public void Configure(EntityTypeBuilder<ServiceOrderJob> builder)
    {
        builder.ToTable("ServiceOrderJobs");

        builder.HasKey(sj => sj.Id);

        builder.Property(sj => sj.ServiceOrderId)
            .IsRequired();

        builder.Property(sj => sj.ServiceJobId)
            .IsRequired();

        builder.Property(sj => sj.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sj => sj.Description)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(sj => sj.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(sj => sj.Status)
            .IsRequired();

        builder.Property(sj => sj.AssignedUserId);

        builder.Property(sj => sj.CreatedAt)
            .IsRequired();

        builder.Property(sj => sj.UpdatedAt);

        builder.Property(sj => sj.CreatedUserId);

        builder.Property(sj => sj.LastUpdatedUserId);

        builder.HasOne(sj => sj.AssignedUser)
            .WithMany()
            .HasForeignKey(sj => sj.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(sj => sj.ServiceJob)
            .WithMany()
            .HasForeignKey(sj => sj.ServiceJobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sj => sj.StatusHistory)
            .WithOne(h => h.ServiceOrderJob)
            .HasForeignKey(h => h.ServiceOrderJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
