using System.Threading;
using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public interface IMainAppService
    {
        Task QueueShutDown(CancellationToken token);
        Task QueueStop(CancellationToken token);
    }
}