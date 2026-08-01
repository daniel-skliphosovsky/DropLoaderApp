using CommunityToolkit.Maui.Storage;
using DropLoaderApp.Services.Interfaces;
using System.Diagnostics;

namespace DropLoaderApp.Services.Pickers;

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

            // User simply dismissed the dialog - not an error.
            if (result.Exception is null)
                return null;

            Debug.WriteLine($"Folder picker failed: {result.Exception}");
            await _dialog.ShowErrorAsync(result.Exception.Message, "Could not pick a folder");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Folder picker threw an exception: {ex}");
            await _dialog.ShowErrorAsync(ex.Message, "Could not pick a folder");
        }

        return null;
    }
}
