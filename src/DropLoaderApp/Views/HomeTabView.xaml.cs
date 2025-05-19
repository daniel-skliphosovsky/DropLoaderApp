namespace DropLoaderApp.Views;

public partial class HomeTabView : ContentView
{
	public HomeTabView()
	{
		InitializeComponent();
	}

    private async void SupportHyperlink_Clicked(object sender, TappedEventArgs e)
    {
        await Launcher.OpenAsync("https://t.me/Daniel_Skliphosovsky");
    }

    private async void TelegramBotHyperlink_Clicked(object sender, TappedEventArgs e)
    {
        await Launcher.OpenAsync("https://t.me/DropLoaderBot");
    }
}
