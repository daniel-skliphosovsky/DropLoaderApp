namespace MediaHub.Services.Interfaces;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string? cancel = null);
    Task ShowErrorAsync(string message, string? title = null);
    Task<bool> ShowConfirmAsync(string title, string message, string? accept = null, string? cancel = null);
}
