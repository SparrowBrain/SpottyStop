using System.Threading.Tasks;

namespace SpottyStop.Services
{
    public class ActionDelayRetriever : IActionDelayRetriever
    {
        private readonly ISpotify _spotify;

        public ActionDelayRetriever(ISpotify spotify)
        {
            _spotify = spotify;
        }

        public async Task<int> GetRemainingSongTimeInMs()
        {
            var context = await _spotify.GetPlayback();
            return context.Item.DurationMs - context.ProgressMs;
        }
    }
}