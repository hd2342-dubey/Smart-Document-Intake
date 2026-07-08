using Bunit;
using IntakeWeb.Shared.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace IntakeTests.IntakeWebTests.Shared.Dialogs;

public class DeleteConfirmationDialogTests
{
    [Fact]
    public async Task Render_ShowsConfirmationMessageAndActions()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        await provider.InvokeAsync(async () =>
            await dialogService.ShowAsync<DeleteConfirmationDialog>("Confirm Deletion", new DialogParameters { ["Id"] = 1 }));

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("Are you sure you want to delete this expense?", provider.Markup);
            Assert.Contains("Yes", provider.Markup);
            Assert.Contains("No", provider.Markup);
        });
    }

    [Fact]
    public async Task ClickYes_ClosesDialogWithOkResult()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        IDialogReference dialogReference = default!;
        await provider.InvokeAsync(async () =>
            dialogReference = await dialogService.ShowAsync<DeleteConfirmationDialog>("Confirm Deletion", new DialogParameters { ["Id"] = 1 }));

        provider.WaitForAssertion(() => Assert.Contains("Yes", provider.Markup));

        provider.FindAll("button").Single(b => b.TextContent.Trim() == "Yes").Click();

        var result = await dialogReference.Result;

        Assert.NotNull(result);
        Assert.False(result.Canceled);
        Assert.Equal(true, result.Data);
    }

    [Fact]
    public async Task ClickNo_CancelsDialog()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        IDialogReference dialogReference = default!;
        await provider.InvokeAsync(async () =>
            dialogReference = await dialogService.ShowAsync<DeleteConfirmationDialog>("Confirm Deletion", new DialogParameters { ["Id"] = 1 }));

        provider.WaitForAssertion(() => Assert.Contains("No", provider.Markup));

        provider.FindAll("button").Single(b => b.TextContent.Trim() == "No").Click();

        var result = await dialogReference.Result;

        Assert.NotNull(result);
        Assert.True(result.Canceled);
    }
}
