using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using SpottyStop.Annotations;
using SpottyStop.Infrastructure;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SpottyStop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly SpotifyLocalAPI _spotify;
        private Track _currentTrack;
        private bool _stopAfterCurrent;
        private bool _shutDownAfterCurrent;
        private string _toolTip;
        private bool _extendedMenu;
        private AfterCurrent _afterCurrent;
        private bool _isConnected;

        public MainWindow()
        {
            InitializeComponent();

            _spotify = new SpotifyLocalAPI();
            _spotify.OnTrackChange += _spotify_OnTrackChange;
        }

        private async void _spotify_OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            await ExecuteAfterCurrentActions();
            UpdateTrack(e.NewTrack);
        }

        private async Task ExecuteAfterCurrentActions()
        {
            if (StopAfterCurrent)
            {
                await _spotify.Pause();
            }

            if (ShutDownAfterCurrent)
            {
                Process.Start("shutdown", "/s /t 10");
                await _spotify.Pause();
            }

            StopAfterCurrent = false;
            ShutDownAfterCurrent = false;
        }

        public void Connect()
        {
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                IsConnected = false;
                RetryConnect();
                return;
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                IsConnected = false;
                RetryConnect();
                return;
            }

            bool successful = _spotify.Connect();
            if (successful)
            {
                IsConnected = true;
                UpdateInfos();
                _spotify.ListenForEvents = true;
            }
            else
            {
                IsConnected = false;
                RetryConnect();
            }
        }

        private void RetryConnect()
        {
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                Connect();
            });
        }

        public void UpdateInfos()
        {
            StatusResponse status = _spotify.GetStatus();
            if (status == null)
                return;

            if (status.Track != null) //Update track infos
                UpdateTrack(status.Track);
        }

        public void UpdateTrack(Track track)
        {
            _currentTrack = track;

            if (track.IsAd())
                return; //Don't process further, maybe null values

            SpotifyUri uri = track.TrackResource.ParseUri();

            SetToolTipText();
        }

        public bool StopAfterCurrent
        {
            get { return _stopAfterCurrent; }
            set
            {
                if (value == _stopAfterCurrent) return;
                _stopAfterCurrent = value;
                SetAfterCurrent();
                SetToolTipText();
                OnPropertyChanged();
            }
        }

        public bool ShutDownAfterCurrent
        {
            get { return _shutDownAfterCurrent; }
            set
            {
                if (value == _shutDownAfterCurrent) return;
                _shutDownAfterCurrent = value;
                SetAfterCurrent();
                SetToolTipText();
                OnPropertyChanged();
            }
        }

        private void SetAfterCurrent()
        {
            if (!IsConnected)
            {
                AfterCurrent = AfterCurrent.NotConnected;
                return;
            }

            if (ShutDownAfterCurrent)
            {
                AfterCurrent = AfterCurrent.ShutDown;
                return;
            }

            if (StopAfterCurrent)
            {
                AfterCurrent = AfterCurrent.Stop;
                return;
            }

            AfterCurrent = AfterCurrent.Nothing;
        }

        public string ToolTipText
        {
            get { return _toolTip; }
            set
            {
                if (value == _toolTip) return;
                _toolTip = value;
                OnPropertyChanged();
            }
        }

        public bool ExtendedMenu
        {
            get { return _extendedMenu; }
            set
            {
                if (value == _extendedMenu) return;
                _extendedMenu = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllowManualConnect));
            }
        }

        public bool AllowManualConnect
        {
            get { return ExtendedMenu && !IsConnected; }
        }

        public ICommand LeftClick
        {
            get { return new RelayCommand(() => { ExtendedMenu = false; }); }
        }

        public AfterCurrent AfterCurrent
        {
            get { return _afterCurrent; }
            set
            {
                if (value == _afterCurrent) return;
                _afterCurrent = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (value == _isConnected) return;
                _isConnected = value;
                SetAfterCurrent();
                SetToolTipText();
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllowManualConnect));
            }
        }

        private void OnClearSelectionClick(object sender, RoutedEventArgs e)
        {
            StopAfterCurrent = false;
            ShutDownAfterCurrent = false;
        }

        private void SetToolTipText()
        {
            if (!IsConnected)
            {
                ToolTipText = "Not connected";
                return;
            }

            if (_currentTrack == null)
            {
                ToolTipText = "Nothing's playing";
                return;
            }

            if (ShutDownAfterCurrent)
            {
                ToolTipText = $"Shutting down after: {_currentTrack.ArtistResource.Name} - {_currentTrack.TrackResource.Name}";
                return;
            }

            if (StopAfterCurrent)
            {
                ToolTipText = $"Stopping after: {_currentTrack.ArtistResource.Name} - {_currentTrack.TrackResource.Name}";
                return;
            }

            ToolTipText = $"{_currentTrack.ArtistResource.Name} - {_currentTrack.TrackResource.Name}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TaskbarIcon_TrayRightMouseDown(object sender, RoutedEventArgs e)
        {
            ExtendedMenu = true;
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            Connect();
        }
    }
}