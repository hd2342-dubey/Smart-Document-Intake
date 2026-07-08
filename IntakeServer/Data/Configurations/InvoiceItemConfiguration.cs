using LibShared.Models.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntakeServer.Data.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("invoice_items", "tracker_schema");

        builder.HasKey(li => li.InvoiceItemId);

        builder.Property(li => li.InvoiceItemId)
            .HasColumnName("invoice_item_id")
            .UseIdentityByDefaultColumn();

        builder.Property(li => li.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(li => li.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(li => li.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(li => li.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(li => li.LineTotal)
            .HasColumnName("line_total")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(li => li.InvoiceId)
            .HasDatabaseName("ix_invoice_items_invoice_id");
    }
}
