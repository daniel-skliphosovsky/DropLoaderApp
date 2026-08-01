using DropLoaderApp.Services.Interfaces;
using System.Diagnostics;

namespace DropLoaderApp.Services.Dialogs;

public sealed class DialogService : IDialogService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = GetCurrentPage();
        if (page is null)
            return;

        try
        {
            await page.DisplayAlert(title, message, cancel);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show alert: {ex}");
        }
    }

    public async Task ShowErrorAsync(string message, string title = "Error")
        => await ShowAlertAsync(title, message, "OK");

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        var page = GetCurrentPage();
        if (page is null)
            return false;

        try
        {
            return await page.DisplayAlert(title, message, accept, cancel);
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
