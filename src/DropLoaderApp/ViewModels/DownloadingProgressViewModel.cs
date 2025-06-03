using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DropLoaderApp.ViewModels
{
	public class DownloadingProgressViewModel : INotifyPropertyChanged
	{
        public DownloadingProgressViewModel()
        {
            CancelDownloadCommand = new Command(CancelDownload);
            CancellationTokenSource = new CancellationTokenSource();
        }

        private float _progress = 0.0f;
        private string _fileName = "";

        private void CancelDownload()
        {
            CancellationTokenSource.Cancel();
        }

        public CancellationTokenSource CancellationTokenSource { get; }

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

        public string DownloadingFileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value.Trim();
                    OnPropertyChanged();
                }
            }
        }

        public Command CancelDownloadCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

