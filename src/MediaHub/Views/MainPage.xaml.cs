using MediaHub.ViewModels;

namespace MediaHub.Views;

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

    // Keeps the content centered vertically inside the ScrollView: the host
    // fills the viewport, so the inner stack floats in the middle and still
    // scrolls when the window gets too small for it.
    private void OnScrollViewSizeChanged(object? sender, EventArgs e)
    {
        if (sender is ScrollView scroll && scroll.Content is Grid host)
            host.MinimumHeightRequest = scroll.Height;
    }
}
