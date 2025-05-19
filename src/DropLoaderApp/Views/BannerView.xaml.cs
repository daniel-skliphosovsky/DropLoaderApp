using DropLoaderApp.ViewModels;

namespace DropLoaderApp.Views;

public partial class BannerView : ContentView
{
	public BannerView()
	{
		InitializeComponent();
	}

    private void ChangeAppTheme(System.Object sender, System.EventArgs e)
    {
		Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
    }

    void HomeTab(System.Object sender, System.EventArgs e)
    {
        var viewModel = (ActiveTabViewModel)BindingContext;
        viewModel.ShowHomeTab();
    }

    void DownloadingTab(System.Object sender, System.EventArgs e)
    {
        var viewModel = (ActiveTabViewModel)BindingContext;
        viewModel.ShowDownloadingTab();
    }
}
