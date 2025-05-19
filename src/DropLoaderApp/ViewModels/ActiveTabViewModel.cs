using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DropLoaderApp.ViewModels
{
    public class ActiveTabViewModel : INotifyPropertyChanged
    {
        private bool _homeTabVisible = true; 
        private bool _downloadingTabVisible;

        public bool HomeTabVisible
        {
            get => _homeTabVisible;
            set
            {
                if (_homeTabVisible != value)
                {
                    _homeTabVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DownloadingTabVisible
        {
            get => _downloadingTabVisible;
            set
            {
                if (_downloadingTabVisible != value)
                {
                    _downloadingTabVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public void ShowHomeTab()
        {
            HomeTabVisible = true;
            DownloadingTabVisible = false;
        }


        public void ShowDownloadingTab()
        {
            HomeTabVisible = false;
            DownloadingTabVisible = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}