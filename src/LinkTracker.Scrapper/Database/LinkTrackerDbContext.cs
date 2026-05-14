using Microsoft.EntityFrameworkCore;
using LinkTracker.Scrapper.Database.Entities;

namespace LinkTracker.Scrapper.Database;

public class LinkTrackerDbContext(DbContextOptions<LinkTrackerDbContext> options) : DbContext(options)
{
    public DbSet<ChatEntity> Chats => Set<ChatEntity>();

    public DbSet<LinkEntity> Links => Set<LinkEntity>();

    public DbSet<ChatLinkEntity> ChatLinks => Set<ChatLinkEntity>();

    public DbSet<TagEntity> Tags => Set<TagEntity>();

    public DbSet<ChatLinkTagEntity> ChatLinkTags => Set<ChatLinkTagEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatEntity>(entity =>
        {
            entity.ToTable("chats");

            entity.HasKey(chat => chat.Id);

            entity.Property(chat => chat.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(chat => chat.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<LinkEntity>(entity =>
        {
            entity.ToTable("links");

            entity.HasKey(link => link.Id);

            entity.Property(link => link.Id)
                .HasColumnName("id");

            entity.Property(link => link.Url)
                .HasColumnName("url")
                .IsRequired();

            entity.Property(link => link.LastCheckedAt)
                .HasColumnName("last_checked_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(link => link.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(link => link.Url)
                .IsUnique();
        });

        modelBuilder.Entity<ChatLinkEntity>(entity =>
        {
            entity.ToTable("chat_links");

            entity.HasKey(chatLink => new { chatLink.ChatId, chatLink.LinkId });

            entity.Property(chatLink => chatLink.ChatId)
                .HasColumnName("chat_id");

            entity.Property(chatLink => chatLink.LinkId)
                .HasColumnName("link_id");

            entity.Property(chatLink => chatLink.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(chatLink => chatLink.Chat)
                .WithMany(chat => chat.ChatLinks)
                .HasForeignKey(chatLink => chatLink.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(chatLink => chatLink.Link)
                .WithMany(link => link.ChatLinks)
                .HasForeignKey(chatLink => chatLink.LinkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TagEntity>(entity =>
        {
            entity.ToTable("tags");

            entity.HasKey(tag => tag.Id);

            entity.Property(tag => tag.Id)
                .HasColumnName("id");

            entity.Property(tag => tag.Name)
                .HasColumnName("name")
                .IsRequired();

            entity.Property(tag => tag.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(tag => tag.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ChatLinkTagEntity>(entity =>
        {
            entity.ToTable("chat_link_tags");

            entity.HasKey(chatLinkTag => new
            {
                chatLinkTag.ChatId,
                chatLinkTag.LinkId,
                chatLinkTag.TagId
            });

            entity.Property(chatLinkTag => chatLinkTag.ChatId)
                .HasColumnName("chat_id");

            entity.Property(chatLinkTag => chatLinkTag.LinkId)
                .HasColumnName("link_id");

            entity.Property(chatLinkTag => chatLinkTag.TagId)
                .HasColumnName("tag_id");

            entity.HasOne(chatLinkTag => chatLinkTag.ChatLink)
                .WithMany(chatLink => chatLink.ChatLinkTags)
                .HasForeignKey(chatLinkTag => new
                {
                    chatLinkTag.ChatId,
                    chatLinkTag.LinkId
                })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(chatLinkTag => chatLinkTag.Tag)
                .WithMany(tag => tag.ChatLinkTags)
                .HasForeignKey(chatLinkTag => chatLinkTag.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}