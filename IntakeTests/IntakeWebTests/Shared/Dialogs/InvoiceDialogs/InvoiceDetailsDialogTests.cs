using Bunit;
using IntakeWeb.Shared.Dialogs.InvoiceDialogs;
using LibShared.Models.Invoices;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace IntakeTests.IntakeWebTests.Shared.Dialogs.InvoiceDialogs;

public class InvoiceDetailsDialogTests
{
    [Fact]
    public async Task Render_ShowsInvoiceHeaderFieldsAndLineItems()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        var invoice = new InvoiceResponse
        {
            InvoiceId = 1,
            InvoiceNumber = "INV-001",
            Supplier = "Contoso",
            InvoiceDate = new DateOnly(2026, 7, 1),
            TotalAmount = 25m,
            SourceFileName = "invoice.json",
            Items =
            [
                new InvoiceItemResponse
                {
                    InvoiceItemId = 10,
                    Description = "Paper",
                    Quantity = 2,
                    UnitPrice = 12.5m,
                    LineTotal = 25m
                }
            ]
        };

        var parameters = new DialogParameters<InvoiceDetailsDialog>();
        parameters.Add(x => x.Invoice, invoice);

        await provider.InvokeAsync(async () =>
            await dialogService.ShowAsync<InvoiceDetailsDialog>("Invoice Details", parameters));

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("Invoice INV-001", provider.Markup);
            Assert.Contains("Contoso", provider.Markup);
            Assert.Contains("01-Jul-2026", provider.Markup);
            Assert.Contains("25.00", provider.Markup);
            Assert.Contains("invoice.json", provider.Markup);
            Assert.Contains("Line Items", provider.Markup);
            Assert.Contains("Paper", provider.Markup);
            Assert.Contains("12.50", provider.Markup);
        });
    }

    [Fact]
    public async Task Render_WhenSourceFileNameIsNull_ShowsDashPlaceholder()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        var invoice = new InvoiceResponse
        {
            InvoiceId = 2,
            InvoiceNumber = "INV-002",
            Supplier = "Fabrikam",
            InvoiceDate = new DateOnly(2026, 6, 15),
            TotalAmount = 100m,
            SourceFileName = null
        };

        var parameters = new DialogParameters<InvoiceDetailsDialog>();
        parameters.Add(x => x.Invoice, invoice);

        await provider.InvokeAsync(async () =>
            await dialogService.ShowAsync<InvoiceDetailsDialog>("Invoice Details", parameters));

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("Source File", provider.Markup);
            Assert.Contains(">-<", provider.Markup.Replace("\n", string.Empty).Replace(" ", string.Empty));
        });
    }

    [Fact]
    public async Task ClickClose_ClosesDialog()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<InvoiceDetailsDialog>();
        parameters.Add(x => x.Invoice, new InvoiceResponse { InvoiceNumber = "INV-003" });

        IDialogReference dialogReference = default!;
        await provider.InvokeAsync(async () =>
            dialogReference = await dialogService.ShowAsync<InvoiceDetailsDialog>("Invoice Details", parameters));

        provider.WaitForAssertion(() => Assert.Contains("Close", provider.Markup));

        provider.FindAll("button").Single(b => b.TextContent.Trim() == "Close").Click();

        var result = await dialogReference.Result;

        Assert.NotNull(result);
        Assert.False(result.Canceled);
    }
}
