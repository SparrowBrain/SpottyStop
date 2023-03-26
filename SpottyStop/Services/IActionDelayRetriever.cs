using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public interface IActionDelayRetriever
    {
        Task<int> GetRemainingSongTimeInMs();
        Task<int> GetRemainingQueueTimeInMs();
    }
}