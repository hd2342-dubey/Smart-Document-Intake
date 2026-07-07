using IntakeServer.Data.Configurations;
using LibShared.Models.Invoices;
using Microsoft.EntityFrameworkCore;

namespace IntakeServer;

public class IntakeDbContext : DbContext
{
    public IntakeDbContext(DbContextOptions<IntakeDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tracker_schema");

        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceItemConfiguration());
    }
}
