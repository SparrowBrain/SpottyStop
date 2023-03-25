using System;
using System.Threading;
using System.Threading.Tasks;
using SpottyStop.Infrastructure.Events;
using Stylet;

namespace SpottyStop.Services
{
    public class GenericDelayedActionRunner : IGenericDelayedActionRunner
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDelayedActionFactory _delayedActionFactory;

        public GenericDelayedActionRunner(
            IEventAggregator eventAggregator,
            IDelayedActionFactory delayedActionFactory)
        {
            _eventAggregator = eventAggregator;
            _delayedActionFactory = delayedActionFactory;
        }

        public async Task InvokeAfterDelayInParallel(Func<Task<int>> getDelayAction, Func<Task> mainAction, CancellationToken token)
        {
            try
            {
                var delay = await getDelayAction.Invoke();
                var delayedAction = _delayedActionFactory.CreateDelayedAction(mainAction, delay, token);

                Task.Run(async () =>
                {
                    await delayedAction.Invoke();
                }, token);
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(new ErrorHappened() { Text = ex.Message });
            }
        }
    }
}