using MediaHub.Services.Interfaces;
using System.Diagnostics;

namespace MediaHub.Services.Dialogs;

public sealed class DialogService : IDialogService
{
    public async Task ShowAlertAsync(string title, string message, string? cancel = null)
    {
        var page = GetCurrentPage();
        if (page is null)
            return;

        try
        {
            await page.DisplayAlert(title, message, cancel ?? Loc.Get("Dialog.Ok"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show alert: {ex}");
        }
    }

    public async Task ShowErrorAsync(string message, string? title = null)
        => await ShowAlertAsync(title ?? Loc.Get("Dialog.Error"), message);

    public async Task<bool> ShowConfirmAsync(string title, string message, string? accept = null, string? cancel = null)
    {
        var page = GetCurrentPage();
        if (page is null)
            return false;

        try
        {
            return await page.DisplayAlert(title, message, accept ?? Loc.Get("Dialog.Yes"), cancel ?? Loc.Get("Dialog.No"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show confirmation dialog: {ex}");
            return false;
        }
    }

    private static Page? GetCurrentPage()
    {
        if (Shell.Current?.CurrentPage is { } page)
            return page;

        return Application.Current?.Windows
            .Select(window => window.Page)
            .OfType<Page>()
            .FirstOrDefault();
    }
}
