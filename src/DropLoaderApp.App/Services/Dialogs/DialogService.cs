using DropLoaderApp.Services.Interfaces;

namespace DropLoaderApp.Services.Dialogs;

public sealed class DialogService : IDialogService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Shell.Current != null)
            await Shell.Current.DisplayAlert(title, message, cancel);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        if (Shell.Current != null)
            return await Shell.Current.DisplayAlert(title, message, accept, cancel);
        return false;
    }
}
