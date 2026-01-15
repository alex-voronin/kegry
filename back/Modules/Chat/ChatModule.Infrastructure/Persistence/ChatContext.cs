using ChatModule.Domain.Aggregates;
using ChatModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatModule.Infrastructure.Persistence;

public class ChatContext : DbContext
{
    public ChatContext(DbContextOptions<ChatContext> options)
        : base(options)
    {
    }

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ActiveChatUserLink> ActiveChatUserLinks => Set<ActiveChatUserLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("meetings");

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.FirstName).HasMaxLength(100);
            builder.Property(x => x.LastName).HasMaxLength(100);
            builder.Property(x => x.AvatarImageS3Key).HasMaxLength(128);

            builder.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Chat>(builder =>
        {
            builder.ToTable("chats");
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.HasQueryFilter(x => !x.IsDeleted);

            builder
                .HasIndex(x => x.Id)
                .HasFilter("\"is_deleted\" = false");
        });

        modelBuilder.Entity<ActiveChatUserLink>(builder =>
        {
            builder.ToTable("active_chat_user_links");
            builder.HasKey(l => l.UserId);
            builder.Property(l => l.UserId).ValueGeneratedNever();

            builder.HasOne(l => l.Chat)
                .WithMany(c => c.UserLinks)
                .HasForeignKey(l => l.ChatId)
                .IsRequired();

            builder
                .HasOne(l => l.User)
                .WithOne(u => u.ChatLink)
                .HasForeignKey<ActiveChatUserLink>(l => l.UserId)
                .IsRequired();
        });

        modelBuilder.Entity<ChatMessage>(builder =>
        {
            builder.ToTable("chat_messages");
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Text).HasMaxLength(ValidationParameters.MaxMessageLength);

            builder.HasOne(x => x.Chat)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}