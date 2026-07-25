using DropLoaderApp.ViewModels;

namespace DropLoaderApp.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel mainVm, DownloadViewModel downloadVm)
    {
        InitializeComponent();
        BindingContext = mainVm;
    }
}
