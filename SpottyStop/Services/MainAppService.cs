using System.Threading;
using System.Threading.Tasks;
using SpottyStop.Infrastructure.Events;
using Stylet;

namespace SpottyStop.Services
{
    public class MainAppService : IMainAppService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IActionDelayRetriever _actionDelayRetriever;
        private readonly IGenericDelayedActionRunner _genericDelayedActionRunner;
        private readonly IComputer _computer;
        private readonly ISpotify _spotify;

        public MainAppService(
            IEventAggregator eventAggregator,
            IActionDelayRetriever actionDelayRetriever,
            IGenericDelayedActionRunner genericDelayedActionRunner,
            IComputer computer,
            ISpotify spotify)
        {
            _eventAggregator = eventAggregator;
            _actionDelayRetriever = actionDelayRetriever;
            _genericDelayedActionRunner = genericDelayedActionRunner;
            _computer = computer;
            _spotify = spotify;
        }

        public async Task QueueShutDown(CancellationToken token)
        {
            await _genericDelayedActionRunner.InvokeAfterDelayInParallel(GetRemainingSongTime, ShutDown, token);
        }

        public async Task QueueStop(CancellationToken token)
        {
            await _genericDelayedActionRunner.InvokeAfterDelayInParallel(GetRemainingSongTime, Stop, token);
        }

        private Task<int> GetRemainingSongTime()
        {
            return _actionDelayRetriever.GetRemainingSongTimeInMs();
        }

        private async Task ShutDown()
        {
            await _spotify.PausePlayback();
            _computer.Shutdown();
            _eventAggregator.PublishOnUIThread(new ShutDownAfterSongHappened());
        }

        private async Task Stop()
        {
            await _spotify.PausePlayback();
            _eventAggregator.PublishOnUIThread(new StopAfterSongHappened());
        }
    }
}