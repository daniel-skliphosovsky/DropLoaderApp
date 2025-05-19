using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DropLoaderApp.ViewModels
{
	public class DownloadingProgressViewModel : INotifyPropertyChanged
	{
        private float _progress = 0.0f;

        public float DownloadingProgress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

