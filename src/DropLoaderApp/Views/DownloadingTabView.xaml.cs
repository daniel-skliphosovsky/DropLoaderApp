using CommunityToolkit.Maui.Storage;
using DropLoaderApp.Downloaders;

namespace DropLoaderApp.Views;

public partial class DownloadingTabView : ContentView
{
    public DownloadingTabView()
    {
        InitializeComponent();
    }
    async void FolderPickButton_Clicked(System.Object sender, System.EventArgs e)
    {
        FolderPickerResult folder = await FolderPicker.Default.PickAsync(default);
        PathEntry.Placeholder = folder.Folder.Path;
        DownloadHandler.DownloadPath = folder.Folder.Path;
    }

    void LinkEntry_TextChanged(System.Object sender, Microsoft.Maui.Controls.TextChangedEventArgs e)
    {
        DownloadHandler.DownloadLink = e.NewTextValue;
    }

    async void DownloadButton_Clicked(System.Object sender, System.EventArgs e)
    {
        Button button = sender as Button;
        button.IsEnabled = false;

        DownloadHandler downloader = new DownloadHandler();
        if (DownloadHandler.DownloadLink != null && DownloadHandler.DownloadPath != null)
        {
           await downloader.TryDownload();
        }
        else
        {
            await Shell.Current.DisplayAlert("Empty fields", "Link or path fields are empty", "Try again");
        }

        button.IsEnabled = true;
    }
}
