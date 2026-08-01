using DropLoader.ViewModels;

namespace DropLoader.Views;

public partial class MainPage : ContentPage
{
    private bool _cardAnimated;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Soft entrance for the main card on first appearance.
        if (!_cardAnimated)
        {
            _cardAnimated = true;
            Card.FadeTo(1, 250, Easing.CubicOut);
        }
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
