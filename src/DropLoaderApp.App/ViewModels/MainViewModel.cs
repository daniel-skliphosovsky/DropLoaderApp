using CommunityToolkit.Mvvm.ComponentModel;

namespace DropLoaderApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedTabIndex;
}
