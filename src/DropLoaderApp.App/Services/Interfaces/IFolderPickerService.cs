namespace DropLoaderApp.Services.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
