using DropLoaderApp.ViewModels;

namespace DropLoaderApp.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // The window is being hidden or closed - stop any running download
        // instead of leaving it to write files in the background.
        if (BindingContext is MainViewModel viewModel)
            viewModel.CancelPending();
    }
}
