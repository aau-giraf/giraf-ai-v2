using Giraf_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Data;

public sealed class GirafAiDbContext(DbContextOptions<GirafAiDbContext> options)
    : DbContext(options)
{
    public DbSet<ImageGenerationQueueEntry> ImageGenerationQueue => Set<ImageGenerationQueueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageGenerationQueueEntry>(entity =>
        {
            entity.ToTable("imggen_queue", "public");
            entity.HasKey(queueEntry => queueEntry.QueueId);

            entity.Property(queueEntry => queueEntry.QueueId)
                .HasColumnName("queue_id")
                .ValueGeneratedOnAdd();
            entity.Property(queueEntry => queueEntry.Prompt)
                .HasColumnName("prompt")
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(queueEntry => queueEntry.ImageId)
                .HasColumnName("image_id")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(queueEntry => queueEntry.MapId)
                .HasColumnName("map_id")
                .IsRequired();
            entity.Property(queueEntry => queueEntry.DatabaseIdType)
                .HasColumnName("id_type")
                .HasColumnType("public.id_type")
                .IsRequired();
            entity.Property(queueEntry => queueEntry.Received)
                .HasColumnName("received")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .IsRequired();
        });
    }
}
