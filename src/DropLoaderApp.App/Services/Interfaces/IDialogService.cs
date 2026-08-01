namespace DropLoaderApp.Services.Interfaces;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task ShowErrorAsync(string message, string title = "Error");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
}
