using Microsoft.EntityFrameworkCore;

using dotnet_html_sortable_table.Models;

namespace dotnet_html_sortable_table.Data;

public class MessagesContext : DbContext
{
    public DbSet<MessageModel> Messages { get; set; } = default!;

    public DbSet<ConversationUserModel> ConversationUsers { get; set; } = default!;

    public MessagesContext(DbContextOptions<MessagesContext> contextOptions) : base(contextOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MessageModel>();

        modelBuilder.Entity<ConversationUserModel>();
    } 
}
