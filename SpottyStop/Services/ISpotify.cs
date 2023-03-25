using System.Threading.Tasks;
using SpotifyAPI.Web.Models;

namespace SpottyStop.Services
{
    public interface ISpotify
    {
        Task<FullTrack> GetPlayingTrack();
        Task<PlaybackContext> GetPlayback();
        Task PausePlayback();
        Task Authenticate();
    }
}