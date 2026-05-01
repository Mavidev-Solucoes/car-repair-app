using CarRepairShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRepairShop.Repository.Configurations;

public class ServiceJobConfiguration : IEntityTypeConfiguration<ServiceJob>
{
    public void Configure(EntityTypeBuilder<ServiceJob> builder)
    {
        builder.ToTable("ServiceJobs");

        builder.HasKey(sj => sj.Id);

        builder.Property(sj => sj.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sj => sj.Description)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(sj => sj.UnitCost)
            .IsRequired();

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

        builder.HasMany(sj => sj.StatusHistory)
            .WithOne(h => h.ServiceJob)
            .HasForeignKey(h => h.ServiceJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
