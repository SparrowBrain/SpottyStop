using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace SpottyStop.Services
{
    public interface ISpotify
    {
        Task<FullTrack> GetPlayingTrack();

        Task<CurrentlyPlayingContext> GetPlayback();

        Task PausePlayback();

        Task Authenticate();
    }
}