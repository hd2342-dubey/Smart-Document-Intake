using LibShared.Models.Invoices;
using Microsoft.EntityFrameworkCore;

namespace IntakeServer.Repositories.Invoices;

public class InvoiceRepository(IntakeDbContext context) : IInvoiceRepository
{
    private readonly IntakeDbContext _context = context;

    public async Task<Invoice?> GetByIdAsync(int invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
    }

    public async Task<Invoice?> GetByNumberAndSupplierAsync(string invoiceNumber, string supplier)
    {
        return await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.InvoiceNumber.ToLower() == invoiceNumber.ToLower() &&
                i.Supplier.ToLower() == supplier.ToLower());
    }

    public async Task<Invoice?> GetByNumberAsync(string invoiceNumber)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceNumber.ToLower() == invoiceNumber.ToLower());
    }

    public async Task<(List<Invoice> Invoices, int TotalCount)> SearchAsync(InvoiceSearchFilter filter)
    {
        IQueryable<Invoice> query = _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Supplier))
            query = query.Where(i => i.Supplier.ToLower().Contains(filter.Supplier.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.InvoiceNumber))
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(filter.InvoiceNumber.ToLower()));

        if (filter.DateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(i => i.TotalAmount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(i => i.TotalAmount <= filter.MaxAmount.Value);

        int totalCount = await query.CountAsync();

        List<Invoice> invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.InvoiceId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (invoices, totalCount);
    }

    public async Task<Invoice> AddAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task DeleteAsync(Invoice invoice)
    {
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task<DashboardResponse> GetDashboardStatsAsync()
    {
        var stats = await _context.Invoices
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalInvoices = g.Count(),
                TotalAmount = g.Sum(i => i.TotalAmount),
                AverageInvoiceAmount = g.Average(i => i.TotalAmount),
                SupplierCount = g.Select(i => i.Supplier).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        List<SupplierTotal> topSuppliers = await _context.Invoices
            .GroupBy(i => i.Supplier)
            .Select(g => new SupplierTotal
            {
                Supplier = g.Key,
                InvoiceCount = g.Count(),
                TotalAmount = g.Sum(i => i.TotalAmount)
            })
            .OrderByDescending(s => s.TotalAmount)
            .Take(5)
            .ToListAsync();

        List<Invoice> recentInvoices = await _context.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(5)
            .ToListAsync();

        return new DashboardResponse
        {
            TotalInvoices = stats?.TotalInvoices ?? 0,
            TotalAmount = stats?.TotalAmount ?? 0,
            AverageInvoiceAmount = Math.Round(stats?.AverageInvoiceAmount ?? 0, 2),
            SupplierCount = stats?.SupplierCount ?? 0,
            TopSuppliers = topSuppliers,
            RecentInvoices = recentInvoices.Select(i => new InvoiceResponse
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                Supplier = i.Supplier,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                SourceFileName = i.SourceFileName,
                CreatedAtUtc = i.CreatedAtUtc
            }).ToList()
        };
    }
}
