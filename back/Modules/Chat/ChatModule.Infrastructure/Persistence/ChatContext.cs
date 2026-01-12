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

            builder
                .HasOne(u => u.ChatLink)
                .WithOne(l => l.User)
                .HasForeignKey<ActiveChatUserLink>(l => l.UserId)
                .IsRequired(false);
        });

        modelBuilder.Entity<Chat>(builder =>
        {
            builder.ToTable("chats");
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder
                .HasMany(c => c.UserLinks)
                .WithOne(l => l.Chat)
                .HasForeignKey(l => l.ChatId);

            builder.HasQueryFilter(x => !x.IsDeleted);

            builder
                .HasIndex(x => x.Id)
                .HasFilter("\"is_deleted\" = false");
        });

        modelBuilder.Entity<ActiveChatUserLink>(builder =>
        {
            builder.ToTable("active_chat_user_links");
            builder.HasKey(x => x.UserId);

            builder.HasOne(uc => uc.Chat)
                .WithMany()
                .HasForeignKey(uc => uc.ChatId);
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
