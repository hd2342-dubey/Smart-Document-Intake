using System.Text;
using Bunit;
using IntakeClient;
using IntakeWeb.Shared.Dialogs.InvoiceDialogs;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace IntakeTests.IntakeWebTests.Shared.Dialogs.InvoiceDialogs;

public class UploadInvoiceDialogTests
{
    [Fact]
    public async Task Render_WithoutSelectedFile_DisablesUploadButton()
    {
        await using var ctx = new BunitContext();
        var (provider, _, _, _) = await ShowDialogAsync(ctx);

        var uploadButton = provider.FindAll("button").Single(b => b.TextContent.Trim() == "Upload");

        Assert.True(uploadButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task SelectingFile_ShowsFileNameAndEnablesUpload()
    {
        await using var ctx = new BunitContext();
        var (provider, _, _, _) = await ShowDialogAsync(ctx);

        await SelectFileAsync(provider, CreateFile("invoice.json", 2048));

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("invoice.json", provider.Markup);
            Assert.Contains("(2 KB)", provider.Markup);

            var uploadButton = provider.FindAll("button").Single(b => b.TextContent.Trim() == "Upload");
            Assert.False(uploadButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task Upload_WhenSuccessful_ShowsSnackbarAndClosesWithInvoice()
    {
        await using var ctx = new BunitContext();
        var (provider, dialogReference, invoiceApi, snackbar) = await ShowDialogAsync(ctx);

        var invoice = new InvoiceResponse { InvoiceId = 1, InvoiceNumber = "INV-001" };
        invoiceApi.UploadAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(InvoiceResponse?, string?)>((invoice, null)));

        await SelectFileAsync(provider, CreateFile("invoice.json", 2048));
        ClickUpload(provider);

        provider.WaitForAssertion(() =>
            snackbar.Received(1).Add("Invoice INV-001 uploaded", Severity.Success));

        var result = await dialogReference.Result;

        Assert.NotNull(result);
        Assert.False(result.Canceled);
        Assert.Same(invoice, result.Data);
        await invoiceApi.Received(1).UploadAsync(Arg.Any<Stream>(), "invoice.json", "application/json");
    }

    [Fact]
    public async Task Upload_WhenApiReturnsError_ShowsErrorAndKeepsDialogOpen()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi, snackbar) = await ShowDialogAsync(ctx);

        invoiceApi.UploadAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(InvoiceResponse?, string?)>((null, "Invoice number is missing")));

        await SelectFileAsync(provider, CreateFile("invoice.json", 2048));
        ClickUpload(provider);

        provider.WaitForAssertion(() => Assert.Contains("Invoice number is missing", provider.Markup));

        // Dialog stays open and no success snackbar is shown.
        Assert.Contains("Upload Invoice Document", provider.Markup);
        snackbar.DidNotReceiveWithAnyArgs().Add(default(string)!);
    }

    [Fact]
    public async Task Upload_WhenFileTooLarge_ShowsValidationErrorWithoutCallingApi()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi, _) = await ShowDialogAsync(ctx);

        await SelectFileAsync(provider, CreateFile("huge.json", 6 * 1024 * 1024));
        ClickUpload(provider);

        provider.WaitForAssertion(() =>
            Assert.Contains("File is too large. Maximum size is 5 MB.", provider.Markup));

        await invoiceApi.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Upload_WhenApiThrows_ShowsFailureMessage()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi, _) = await ShowDialogAsync(ctx);

        invoiceApi.UploadAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromException<(InvoiceResponse?, string?)>(new InvalidOperationException("Network down")));

        await SelectFileAsync(provider, CreateFile("invoice.json", 2048));
        ClickUpload(provider);

        provider.WaitForAssertion(() =>
            Assert.Contains("Upload failed: Network down", provider.Markup));
    }

    [Fact]
    public async Task ClickCancel_CancelsDialog()
    {
        await using var ctx = new BunitContext();
        var (provider, dialogReference, _, _) = await ShowDialogAsync(ctx);

        provider.FindAll("button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        var result = await dialogReference.Result;

        Assert.NotNull(result);
        Assert.True(result.Canceled);
    }

    private static async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference DialogReference, IInvoiceApiClient InvoiceApi, ISnackbar Snackbar)> ShowDialogAsync(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        IDialogReference dialogReference = default!;
        await provider.InvokeAsync(async () =>
            dialogReference = await dialogService.ShowAsync<UploadInvoiceDialog>("Upload Invoice"));

        provider.WaitForAssertion(() => Assert.Contains("Upload Invoice Document", provider.Markup));

        return (provider, dialogReference, invoiceApi, snackbar);
    }

    private static IBrowserFile CreateFile(string name, long size)
    {
        IBrowserFile file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.Size.Returns(size);
        file.ContentType.Returns("application/json");
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes("{}")));
        return file;
    }

    private static async Task SelectFileAsync(IRenderedComponent<MudDialogProvider> provider, IBrowserFile file)
    {
        var fileUpload = provider.FindComponent<MudFileUpload<IBrowserFile>>();
        await provider.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(file));
    }

    private static void ClickUpload(IRenderedComponent<MudDialogProvider> provider)
    {
        provider.FindAll("button").Single(b => b.TextContent.Trim() == "Upload").Click();
    }
}
