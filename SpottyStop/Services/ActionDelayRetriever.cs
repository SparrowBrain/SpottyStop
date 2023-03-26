using System;
using System.Threading.Tasks;
using SpotifyAPI.Web;

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
            var track = context.Item as FullTrack;
            if (track == null)
            {
                throw new Exception("Not a track");
            }

            return track.DurationMs - context.ProgressMs;
        }
    }
}