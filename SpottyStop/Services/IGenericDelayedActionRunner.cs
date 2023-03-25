using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public interface IGenericDelayedActionRunner
    {
        Task InvokeAfterDelayInParallel(Func<Task<int>> getDelayAction, Func<Task> mainAction, CancellationToken token);
    }
}