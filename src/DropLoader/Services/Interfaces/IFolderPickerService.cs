namespace DropLoader.Services.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
