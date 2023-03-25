using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public interface IDelayedActionFactory
    {
        Func<Task> CreateDelayedAction(Func<Task> action, int delayInMilliseconds, CancellationToken ct);
    }
}