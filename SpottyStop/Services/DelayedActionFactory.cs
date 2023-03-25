using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public class DelayedActionFactory : IDelayedActionFactory
    {
        public Func<Task> CreateDelayedAction(Func<Task> action, int delayInMilliseconds, CancellationToken ct)
        {
            return async () =>
            {
                try
                {
                    await Task.Delay(delayInMilliseconds, ct);
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    await action.Invoke();
                }
                catch (OperationCanceledException ex)
                {
                }
            };
        }
    }
}