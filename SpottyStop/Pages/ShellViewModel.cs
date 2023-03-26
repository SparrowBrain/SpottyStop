using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SpottyStop.Infrastructure;
using SpottyStop.Infrastructure.Events;
using SpottyStop.Services;
using Stylet;

namespace SpottyStop.Pages
{
    public class ShellViewModel : Screen,
        IHandle<ErrorHappened>,
        IHandle<ShutDownAfterSongHappened>,
        IHandle<StopAfterSongHappened>,
        IHandle<ShutDownAfterQueueHappened>,
        IHandle<StopAfterQueueHappened>
    {
        private readonly ISpotify _spotify;
        private readonly IMainAppService _mainAppService;
        private bool _stopAfterCurrent;
        private bool _shutDownAfterCurrent;
        private bool _stopAfterQueue;
        private bool _shutDownAfterQueue;
        private string _toolTip;
        private bool _extendedMenu;
        private AppState _appState;

        private CancellationTokenSource _stopAfterCurrentCancellationSource;
        private CancellationTokenSource _shutDownAfterCurrentCancellationSource;
        private CancellationTokenSource _stopAfterQueueCancellationSource;
        private CancellationTokenSource _shutDownAfterQueueCancellationSource;

        public ShellViewModel(ISpotify spotify, IEventAggregator eventAggregator, IMainAppService mainAppService)
        {
            _spotify = spotify;
            _mainAppService = mainAppService;
            eventAggregator.Subscribe(this);

            _stopAfterCurrentCancellationSource = new CancellationTokenSource();
            _shutDownAfterCurrentCancellationSource = new CancellationTokenSource();
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
                    _stopAfterCurrentCancellationSource = new CancellationTokenSource();
                    _mainAppService.ScheduleStopAfterCurrent(_stopAfterCurrentCancellationSource.Token);
                }
                else
                {
                    _stopAfterCurrentCancellationSource.Cancel();
                }

                SetAppState();
                Task.Run(SetToolTipText);
                NotifyOfPropertyChange();
            }
        }

        public bool ShutDownAfterCurrent
        {
            get { return _shutDownAfterCurrent; }
            set
            {
                if (value == _shutDownAfterCurrent)
                {
                    return;
                }

                _shutDownAfterCurrent = value;
                if (_shutDownAfterCurrent)
                {
                    _shutDownAfterCurrentCancellationSource = new CancellationTokenSource();
                    _mainAppService.ScheduleShutdownAfterCurrent(_shutDownAfterCurrentCancellationSource.Token);
                }
                else
                {
                    _shutDownAfterCurrentCancellationSource.Cancel();
                }

                SetAppState();
                Task.Run(SetToolTipText);
                NotifyOfPropertyChange();
            }
        }

        public bool StopAfterQueue
        {
            get { return _stopAfterQueue; }
            set
            {
                if (value == _stopAfterQueue) return;
                _stopAfterQueue = value;
                if (_stopAfterQueue)
                {
                    _stopAfterQueueCancellationSource = new CancellationTokenSource();
                    _mainAppService.ScheduleStopAfterQueue(_stopAfterQueueCancellationSource.Token);
                }
                else
                {
                    _stopAfterQueueCancellationSource.Cancel();
                }

                SetAppState();
                Task.Run(SetToolTipText);
                NotifyOfPropertyChange();
            }
        }

        public bool ShutDownAfterQueue
        {
            get { return _shutDownAfterQueue; }
            set
            {
                if (value == _shutDownAfterQueue)
                {
                    return;
                }

                _shutDownAfterQueue = value;
                if (_shutDownAfterQueue)
                {
                    _shutDownAfterQueueCancellationSource = new CancellationTokenSource();
                    _mainAppService.ScheduleShutdownAfterQueue(_shutDownAfterQueueCancellationSource.Token);
                }
                else
                {
                    _shutDownAfterQueueCancellationSource.Cancel();
                }

                SetAppState();
                Task.Run(SetToolTipText);
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
            StopAfterQueue = false;
            ShutDownAfterQueue = false;
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

        public void Handle(ShutDownAfterSongHappened message)
        {
            ShutDownAfterCurrent = false;
        }

        public void Handle(StopAfterSongHappened message)
        {
            StopAfterCurrent = false;
        }

        public void Handle(ShutDownAfterQueueHappened message)
        {
            ShutDownAfterQueue = false;
        }

        public void Handle(StopAfterQueueHappened message)
        {
            StopAfterQueue = false;
        }

        private async Task SetToolTipText()
        {
            if (ShutDownAfterCurrent)
            {
                var track = await _spotify.GetPlayingTrack();
                ToolTipText = $"Shutting down after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            if (ShutDownAfterQueue)
            {
                ToolTipText = $"Shutting down after queue";
                return;
            }

            if (StopAfterCurrent)
            {
                var track = await _spotify.GetPlayingTrack();
                ToolTipText = $"Stopping after: {track.Artists[0].Name} - {track.Name}";
                return;
            }

            if (StopAfterQueue)
            {
                ToolTipText = $"Stopping after queue";
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

            if (ShutDownAfterQueue)
            {
                AppState = AppState.ShutDownAfterQueue;
                return;
            }

            if (StopAfterCurrent)
            {
                AppState = AppState.StopAfterCurrent;
                return;
            }

            if (StopAfterQueue)
            {
                AppState = AppState.StopAfterQueue;
                return;
            }

            AppState = AppState.Nothing;
        }
    }
}