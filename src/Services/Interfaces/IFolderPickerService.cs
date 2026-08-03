namespace MediaHub.Services.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
