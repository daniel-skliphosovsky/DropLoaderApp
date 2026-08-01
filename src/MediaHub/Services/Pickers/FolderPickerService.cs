using CommunityToolkit.Maui.Storage;
using MediaHub.Services.Interfaces;
using System.Diagnostics;

namespace MediaHub.Services.Pickers;

public sealed class FolderPickerService : IFolderPickerService
{
    private readonly IDialogService _dialog;

    public FolderPickerService(IDialogService dialog)
    {
        _dialog = dialog;
    }

    public async Task<string?> PickFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync();
            if (result.IsSuccessful && result.Folder is not null)
                return result.Folder.Path;

            // User cancelled the dialog - not an error.
            return null;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the dialog - not an error.
            return null;
        }
        catch (FolderPickerException)
        {
            // macOS throws this when the user cancels the native panel.
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Folder picker failed: {ex}");
            await _dialog.ShowErrorAsync(ex.Message, Loc.Get(LocKeys.DialogPickFolderError));
            return null;
        }
    }
}
