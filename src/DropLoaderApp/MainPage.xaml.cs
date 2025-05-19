using DropLoaderApp.ViewModels;
namespace DropLoaderApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = new ActiveTabViewModel();
	}
	
}


