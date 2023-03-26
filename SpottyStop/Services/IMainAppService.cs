using System.Threading;
using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public interface IMainAppService
    {
        Task ScheduleShutdownAfterCurrent(CancellationToken token);
        Task ScheduleStopAfterCurrent(CancellationToken token); 
        Task ScheduleShutdownAfterQueue(CancellationToken token);
        Task ScheduleStopAfterQueue(CancellationToken token);
    }
}