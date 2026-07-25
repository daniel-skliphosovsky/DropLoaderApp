using DropLoaderApp.Services.Interfaces;
using CommunityToolkit.Maui.Storage;

namespace DropLoaderApp.Services.Pickers;

public sealed class FolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync();
            if (result.IsSuccessful && result.Folder != null)
                return result.Folder.Path;
        }
        catch
        {
            // ignored
        }
        return null;
    }
}
