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

        private CancellationTokenSource _stopCancellationSource;
        private CancellationTokenSource _shutDownCancellationSource;

        public MainWindow()
        {
            InitializeComponent();

            _stopCancellationSource = new CancellationTokenSource();
            _shutDownCancellationSource = new CancellationTokenSource();
        }

        private async Task Stop()
        {
            await TrySpotify(() => _spotify.PausePlayback());
            StopAfterCurrent = false;
        }

        private async Task ShutDown()
        {
            await TrySpotify(() => _spotify.PausePlayback());
            Process.Start("shutdown", "/s /t 10");
            ShutDownAfterCurrent = false;
        }

        public async Task Connect()
        {
            _spotify = await RunAuthentication();
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
                SetToolTipText().Wait();
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
                SetToolTipText().Wait();
                OnPropertyChanged();
            }
        }

        private void SetAfterCurrent()
        {
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

        private void QueueAction(Func<Task> action, ref CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource = new CancellationTokenSource();

            var playbackContext = TrySpotify(() => _spotify.GetPlayback()).Result;

            Task.Factory.StartNew(async x =>
            {
                var token = (CancellationToken)x;

                var songDuration = playbackContext.Item.DurationMs;
                var progressMs = playbackContext.ProgressMs;
                var timeLeft = songDuration - progressMs;

                await Task.Delay(timeLeft, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await action.Invoke();
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
            }
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

        private void OnClearSelectionClick(object sender, RoutedEventArgs e)
        {
            StopAfterCurrent = false;
            ShutDownAfterCurrent = false;
        }

        private async Task SetToolTipText()
        {
            if (ShutDownAfterCurrent)
            {
                var track = await TrySpotify(() => _spotify.GetPlayingTrack().Item);
                ToolTipText = $"Shutting down after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            if (StopAfterCurrent)
            {
                var track = await TrySpotify(() => _spotify.GetPlayingTrack().Item);
                ToolTipText = $"Stopping after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            ToolTipText = "All is good";
        }

        private async Task<T> TrySpotify<T>(Func<T> spotifyAction)
        {
            try
            {
                return spotifyAction.Invoke();
            }
            catch
            {
                try
                {
                    await Connect();
                    return spotifyAction.Invoke();
                }
                catch (Exception ex)
                {
                    ToolTipText = ex.Message;
                    AfterCurrent = AfterCurrent.NotConnected;
                    throw;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AfterCurrent = AfterCurrent.Nothing;
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TaskbarIcon_TrayRightMouseDown(object sender, RoutedEventArgs e)
        {
            ExtendedMenu = true;
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