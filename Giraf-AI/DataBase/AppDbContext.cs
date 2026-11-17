using Giraf_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ImgGenQueue> ImgGenQueues { get; set; } = null!;
    public DbSet<TtsQueue> TtsQueues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map the PostgreSQL enum
        modelBuilder.HasPostgresEnum<IdType>("id_type");
        modelBuilder.HasPostgresEnum<LangType>("origin_type");

        modelBuilder.Entity<ImgGenQueue>(entity =>
        {
            entity.ToTable("imggen_queue");
            entity.HasKey(e => e.QueueId);
            entity.Property(e => e.QueueId).HasColumnName("queue_id").ValueGeneratedOnAdd();
            entity.Property(e => e.Prompt).HasColumnName("prompt").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ImageId).HasColumnName("image_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.MapId).HasColumnName("map_id").IsRequired();
            entity.Property(e => e.IdType).HasColumnName("id_type").HasColumnType("id_type").IsRequired();
            entity.Property(e => e.Received).HasColumnName("received").IsRequired();
        });

        modelBuilder.Entity<TtsQueue>(entity =>
        {
            entity.ToTable("tts_queue");
            entity.HasKey(e => e.TtsId);
            entity.Property(e => e.Prompt).HasColumnName("prompt").HasMaxLength(500).IsRequired();
            entity.Property(e => e.TtsId).HasColumnName("tts_id").IsRequired();
            entity.Property(e => e.Lang).HasColumnName("language").HasColumnType("language").IsRequired();
            entity.Property(e => e.Received).HasColumnName("received").IsRequired();
        });
    }
}
