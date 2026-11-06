using Giraf_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ImgGenQueue> ImgGenQueues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map the PostgreSQL enum
        modelBuilder.HasPostgresEnum<IdType>("id_type");

        modelBuilder.Entity<ImgGenQueue>(entity =>
        {
            entity.ToTable("imggen_queue");

            entity.HasKey(e => e.QueueId);

            entity.Property(e => e.QueueId)
                .HasColumnName("queue_id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Prompt)
                .HasColumnName("prompt")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.TranslatedPrompt)
                .HasColumnName("translated_prompt")
                .HasMaxLength(500);

            entity.Property(e => e.ImageId)
                .HasColumnName("image_id")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.MapId)
                .HasColumnName("map_id")
                .IsRequired();

            entity.Property(e => e.IdType)
                .HasColumnName("id_type")
                .HasColumnType("id_type")
                .IsRequired();

            entity.Property(e => e.TitleDa)
                .HasColumnName("title_da")
                .HasMaxLength(100);

            entity.Property(e => e.TitleEn)
                .HasColumnName("title_en")
                .HasMaxLength(100);

            entity.Property(e => e.Nsfw)
                .HasColumnName("nsfw");

            entity.Property(e => e.Received)
                .HasColumnName("received")
                .IsRequired();

            entity.Property(e => e.Finished)
                .HasColumnName("finished");
        });
    }
}