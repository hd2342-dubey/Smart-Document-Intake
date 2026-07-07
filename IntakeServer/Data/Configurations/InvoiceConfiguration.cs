using LibShared.Models.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntakeServer.Data.Configurations;

/// <summary>
/// Fluent API mapping for the invoices table.
/// </summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", "tracker_schema");

        builder.HasKey(i => i.InvoiceId);

        builder.Property(i => i.InvoiceId)
            .HasColumnName("invoice_id")
            .UseIdentityByDefaultColumn();

        builder.Property(i => i.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Supplier)
            .HasColumnName("supplier")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.InvoiceDate)
            .HasColumnName("invoice_date")
            .IsRequired();

        builder.Property(i => i.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.SourceFileName)
            .HasColumnName("source_file_name")
            .HasMaxLength(260);

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Duplicate detection: an invoice number must be unique per supplier.
        builder.HasIndex(i => new { i.InvoiceNumber, i.Supplier })
            .IsUnique()
            .HasDatabaseName("ux_invoices_number_supplier");

        builder.HasIndex(i => i.Supplier)
            .HasDatabaseName("ix_invoices_supplier");

        builder.HasIndex(i => i.InvoiceDate)
            .HasDatabaseName("ix_invoices_invoice_date");

        builder.HasMany(i => i.Items)
            .WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
