using MediaHub.Services.Interfaces;
using MediaHub.Services.Logging;

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
            await page.DisplayAlert(title, message, cancel ?? Loc.Get(LocKeys.DialogOk));
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex);
        }
    }

    public async Task ShowErrorAsync(string message, string? title = null)
        => await ShowAlertAsync(title ?? Loc.Get(LocKeys.DialogError), message);

    public async Task<bool> ShowConfirmAsync(string title, string message, string? accept = null, string? cancel = null)
    {
        var page = GetCurrentPage();
        if (page is null)
            return false;

        try
        {
            return await page.DisplayAlert(title, message, accept ?? Loc.Get(LocKeys.DialogYes), cancel ?? Loc.Get(LocKeys.DialogNo));
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex);
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
