using Microsoft.EntityFrameworkCore;

using dotnet_html_sortable_table.Models;

namespace dotnet_html_sortable_table.Data;

public class SqliteContext : DbContext
{

    public DbSet<DemoTable> Entries { get; set; } = default!;
    public DbSet<DemoObject> TableContainer { get; set; } = default!;
    public DbSet<Accounts> Accounts { get; set; } = default!;

    public SqliteContext(DbContextOptions<SqliteContext> contextOptions) : base(contextOptions) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DemoObject>()
            .HasMany(m => m.Table)
            .WithOne(m => m.DemoObject)
            .HasForeignKey(m => m.DemoObjectId)
            .IsRequired();

        modelBuilder.Entity<DemoObject>()
            .HasData(new DemoObject
            {
                Id = 1,
                IdSort = false,
                RandIntSort = false,
                NameSort = false,
            });

        modelBuilder.Entity<DemoTable>()
            .HasData(new DemoTable[] {
                    new() {
                        Id = 1,
                        RandInt = 3,
                        Name = "Bill",
                        DemoObjectId = 1
                    },

                    new() {
                        Id = 2,
                        RandInt = 9,
                        Name = "Bob",
                        DemoObjectId = 1
                    },

                    new() {
                        Id = 3,
                        RandInt = 0,
                        Name = "Jim",
                        DemoObjectId = 1
                    },
                });
    }
}
