using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SpottyStop.Infrastructure;
using SpottyStop.Infrastructure.Events;
using SpottyStop.Services;
using Stylet;

namespace SpottyStop.Pages
{
    public class ShellViewModel : Screen, IHandle<ErrorHappened>
    {
        private readonly ISpotify _spotify;
        private bool _stopAfterCurrent;
        private bool _shutDownAfterCurrent;
        private string _toolTip;
        private bool _extendedMenu;
        private AppState _appState;

        private CancellationTokenSource _stopCancellationSource;
        private CancellationTokenSource _shutDownCancellationSource;

        public ShellViewModel(ISpotify spotify, IEventAggregator eventAggregator)
        {
            _spotify = spotify;
            eventAggregator.Subscribe(this);

            _stopCancellationSource = new CancellationTokenSource();
            _shutDownCancellationSource = new CancellationTokenSource();
        }

        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();

            AppState = AppState.Nothing;
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

        public void ShowExtendedMenu()
        {
            ExtendedMenu = true;
        }

        public void Handle(ErrorHappened message)
        {
            ToolTipText = message.Text;
            AppState = AppState.NotConnected;
        }

        private async Task Stop()
        {
            await _spotify.PausePlayback();
            StopAfterCurrent = false;
        }

        private async Task ShutDown()
        {
            await _spotify.PausePlayback();
            Process.Start("shutdown", "/s /t 10");
            ShutDownAfterCurrent = false;
        }

        private async Task SetToolTipText()
        {
            if (ShutDownAfterCurrent)
            {
                var track = await _spotify.GetPlayingTrack();
                ToolTipText = $"Shutting down after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            if (StopAfterCurrent)
            {
                var track = await _spotify.GetPlayingTrack();
                ToolTipText = $"Stopping after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            ToolTipText = "All is good";
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

            var playbackContext = _spotify.GetPlayback().Result;

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
    }
}