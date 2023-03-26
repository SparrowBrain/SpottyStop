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
        IHandle<StopAfterSongHappened>
    {
        private readonly ISpotify _spotify;
        private readonly IMainAppService _mainAppService;
        private bool _stopAfterCurrent;
        private bool _shutDownAfterCurrent;
        private string _toolTip;
        private bool _extendedMenu;
        private AppState _appState;

        private CancellationTokenSource _stopCancellationSource;
        private CancellationTokenSource _shutDownCancellationSource;

        public ShellViewModel(ISpotify spotify, IEventAggregator eventAggregator, IMainAppService mainAppService)
        {
            _spotify = spotify;
            _mainAppService = mainAppService;
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
                    _stopCancellationSource = new CancellationTokenSource();
                    _mainAppService.QueueStop(_stopCancellationSource.Token);
                }
                else
                {
                    _stopCancellationSource.Cancel();
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
                    _shutDownCancellationSource = new CancellationTokenSource();
                    _mainAppService.QueueShutDown(_shutDownCancellationSource.Token);
                }
                else
                {
                    _shutDownCancellationSource.Cancel();
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
    }
}