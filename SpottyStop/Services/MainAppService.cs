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

        public async Task ScheduleShutdownAfterCurrent(CancellationToken token)
        {
            await _genericDelayedActionRunner.InvokeAfterDelayInParallel(GetRemainingSongTime, ShutDown<ShutDownAfterSongHappened>, token);
        }

        public async Task ScheduleStopAfterCurrent(CancellationToken token)
        {
            await _genericDelayedActionRunner.InvokeAfterDelayInParallel(GetRemainingSongTime, Stop<StopAfterSongHappened>, token);
        }

        public async Task ScheduleShutdownAfterQueue(CancellationToken token)
        {
            await _genericDelayedActionRunner.InvokeAfterDelayInParallel(GetRemainingQueueTime, ShutDown<ShutDownAfterQueueHappened>, token);
        }

        public async Task ScheduleStopAfterQueue(CancellationToken token)
        {
            await _genericDelayedActionRunner.InvokeAfterDelayInParallel(GetRemainingQueueTime, Stop<StopAfterQueueHappened>, token);
        }

        private Task<int> GetRemainingSongTime()
        {
            return _actionDelayRetriever.GetRemainingSongTimeInMs();
        }

        private Task<int> GetRemainingQueueTime()
        {
            return _actionDelayRetriever.GetRemainingQueueTimeInMs();
        }

        private async Task ShutDown<T>() where T : new() 
        {
            await _spotify.PausePlayback();
            _computer.Shutdown();
            _eventAggregator.PublishOnUIThread(new T());
        }

        private async Task Stop<T>() where T : new() 
        {
            await _spotify.PausePlayback();
            _eventAggregator.PublishOnUIThread(new T());
        }
    }
}