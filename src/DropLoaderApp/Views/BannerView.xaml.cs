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
        if (Application.Current.UserAppTheme == AppTheme.Unspecified)
            Application.Current.UserAppTheme = AppInfo.RequestedTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        else Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
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
