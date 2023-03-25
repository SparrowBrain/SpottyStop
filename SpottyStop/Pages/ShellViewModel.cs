using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpottyStop.Infrastructure;
using Stylet;

namespace SpottyStop.Pages
{
    public class ShellViewModel : Screen
    {
        private SpotifyWebAPI _spotify;
        private bool _stopAfterCurrent;
        private bool _shutDownAfterCurrent;
        private string _toolTip;
        private bool _extendedMenu;
        private AppState _appState;

        private CancellationTokenSource _stopCancellationSource;
        private CancellationTokenSource _shutDownCancellationSource;

        public ShellViewModel()
        {
            _stopCancellationSource = new CancellationTokenSource();
            _shutDownCancellationSource = new CancellationTokenSource();
        }

        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();

            AppState = AppState.Nothing;
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

        public async Task Authenticate()
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

                SetAppState();
                SetToolTipText().Wait();
                NotifyOfPropertyChange();
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

                SetAppState();
                SetToolTipText().Wait();
                NotifyOfPropertyChange();
            }
        }

        private void SetAppState()
        {
            if (ShutDownAfterCurrent)
            {
                AppState = AppState.ShutDownAfterCurrent;
                return;
            }

            if (StopAfterCurrent)
            {
                AppState = AppState.StopAfterCurrent;
                return;
            }

            AppState = AppState.Nothing;
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
            get => _toolTip;
            set => SetAndNotify(ref _toolTip, value);
        }

        public bool ExtendedMenu
        {
            get => _extendedMenu;
            set => SetAndNotify(ref _extendedMenu, value);
        }

        public ICommand LeftClick
        {
            get { return new RelayCommand(() => { ExtendedMenu = false; }); }
        }

        public AppState AppState
        {
            get => _appState;
            set => SetAndNotify(ref _appState, value);
        }

        public void ClearSelectionClick()
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

        private async Task<T> TrySpotify<T>(Func<T> spotifyAction) where T : BasicModel
        {
            try
            {
                if (_spotify == null)
                {
                    await Authenticate();
                }

                var result = spotifyAction.Invoke();
                if (result.HasError() && result.Error.Status == 401)
                {
                    await Authenticate();
                    result = spotifyAction.Invoke();
                }

                return result;
            }
            catch (Exception ex)
            {
                ToolTipText = ex.Message;
                AppState = AppState.NotConnected;
                throw;
            }
        }

        public void ShowExtendedMenu()
        {
            ExtendedMenu = true;
        }

        private void TaskbarIcon_TrayRightMouseDown(object sender, RoutedEventArgs e)
        {
            
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