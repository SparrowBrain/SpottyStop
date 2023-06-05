using System;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpottyStop.Infrastructure.Events;
using Stylet;
using FullTrack = SpotifyAPI.Web.Models.FullTrack;

namespace SpottyStop.Services
{
    internal class Spotify : ISpotify
    {
        private SpotifyWebAPI _spotifyWebApi;
        private readonly IEventAggregator _eventAggregator;

        public Spotify(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public async Task<FullTrack> GetPlayingTrack()
        {
            return await TrySpotify(() => _spotifyWebApi.GetPlayingTrack().Item);
        }

        public async Task<PlaybackContext> GetPlayback()
        {
            return await TrySpotify(() => _spotifyWebApi.GetPlayback());
        }

        public async Task PausePlayback()
        {
            await TrySpotify(() => _spotifyWebApi.PausePlayback());
        }

        private async Task<SpotifyWebAPI> RunAuthentication()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                "http://localhost",
                8000,
                "1bb1fc7f880443138e22068f49da7446",
                Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState);

            try
            {
                return await webApiFactory.GetWebApi();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _eventAggregator.PublishOnUIThread(new ErrorHappened() { Text = ex.Message });
                throw;
            }
        }

        public async Task Authenticate()
        {
            _spotifyWebApi = await RunAuthentication();
        }

        private async Task<T> TrySpotify<T>(Func<T> spotifyAction) where T : BasicModel
        {
            try
            {
                if (_spotifyWebApi == null)
                {
                    await Authenticate();
                }

                var result = spotifyAction.Invoke();
                if (result.HasError() && result.Error.Status == 401)
                {
                    await Authenticate();
                    result = spotifyAction.Invoke();
                }

                return result;
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(new ErrorHappened { Text = ex.Message });
                throw;
            }
        }
    }
}