using SpotifyAPI.Local;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpottyStop.Annotations;
using SpottyStop.Infrastructure;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private SpotifyWebAPI _spotify;
        private bool _stopAfterCurrent;
        private bool _shutDownAfterCurrent;
        private string _toolTip;
        private bool _extendedMenu;
        private AfterCurrent _afterCurrent;
        private bool _isConnected;

        private CancellationTokenSource _stopCancellationSource;
        private CancellationTokenSource _shutDownCancellationSource;
        private CancellationTokenSource _retryConnectCancellationSource;

        public MainWindow()
        {
            InitializeComponent();

            _stopCancellationSource = new CancellationTokenSource();
            _shutDownCancellationSource = new CancellationTokenSource();
            _retryConnectCancellationSource = new CancellationTokenSource();
        }

        private void Stop()
        {
            _spotify.PausePlayback();
            StopAfterCurrent = false;
        }

        private void ShutDown()
        {
            _spotify.PausePlayback();
            Process.Start("shutdown", "/s /t 10");
            ShutDownAfterCurrent = false;
        }

        public async Task Connect()
        {
            _retryConnectCancellationSource.Cancel();
            _retryConnectCancellationSource = new CancellationTokenSource();

            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                IsConnected = false;
                await RetryConnect();
                return;
            }

            bool successful;
            try
            {
                _spotify = await RunAuthentication();
                successful = true;
            }
            catch
            {
                successful = false;
            }

            if (successful)
            {
                IsConnected = true;
                SetToolTipText();
            }
            else
            {
                IsConnected = false;
                await RetryConnect();
            }
        }

        private async Task RetryConnect()
        {
            var cancellationToken = _retryConnectCancellationSource.Token;
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Connect();
        }

        public bool StopAfterCurrent
        {
            get { return _stopAfterCurrent; }
            set
            {
                if (value == _stopAfterCurrent) return;
                _stopAfterCurrent = value;
                if (_stopAfterCurrent)
                {
                    QueueAction(Stop, ref _stopCancellationSource);
                }
                else
                {
                    _stopCancellationSource.Cancel();
                }

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
                if (_shutDownAfterCurrent)
                {
                    QueueAction(ShutDown, ref _shutDownCancellationSource);
                }
                else
                {
                    _shutDownCancellationSource.Cancel();
                }

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

        private void QueueAction(Action action, ref CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(async x =>
            {
                var token = (CancellationToken)x;
                var progressMs = _spotify.GetPlayback().ProgressMs;
                var timeLeft = _spotify.GetPlayback().Item.DurationMs - progressMs;

                await Task.Delay(timeLeft, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                action.Invoke();
            }, cancellationTokenSource.Token, cancellationTokenSource.Token);
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

            if (ShutDownAfterCurrent)
            {
                var track = _spotify.GetPlayingTrack().Item;
                ToolTipText = $"Shutting down after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            if (StopAfterCurrent)
            {
                var track = _spotify.GetPlayingTrack().Item;
                ToolTipText = $"Stopping after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            ToolTipText = "All is good";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(async () => { await Connect(); });
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
            Task.Factory.StartNew(async () => { await Connect(); });
        }

        private static async Task<SpotifyWebAPI> RunAuthentication()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                "http://localhost",
                8000,
                "1bb1fc7f880443138e22068f49da7446",
                Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState);

            try
            {
                return await webApiFactory.GetWebApi();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}