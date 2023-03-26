using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpottyStop.Infrastructure.Events;
using Stylet;

namespace SpottyStop.Services
{
    internal class Spotify : ISpotify
    {
        private const string ClientId = "1bb1fc7f880443138e22068f49da7446";
        private const string RefreshTokenFile = "token.json";
        private SemaphoreSlim _authenticationStartSemaphore = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _authenticationInProgressSemaphore;
        private string _verifier;
        private SpotifyClient _spotifyClient;
        private readonly IEventAggregator _eventAggregator;
        private Regex _regex = new Regex(@"\/\?code=(?<code>[^\s]+)");

        public Spotify(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public async Task<FullTrack> GetPlayingTrack()
        {
            return await TrySpotify(async () =>
            {
                var track = (FullTrack)(await _spotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track))).Item;
                return track;
            });
        }

        public async Task<CurrentlyPlayingContext> GetPlayback()
        {
            return await TrySpotify(() => _spotifyClient.Player.GetCurrentPlayback());
        }

        public async Task<QueueResponse> GetQueue()
        {
            return await TrySpotify(() => _spotifyClient.Player.GetQueue());
        }

        public async Task PausePlayback()
        {
            await TrySpotify(() => _spotifyClient.Player.PausePlayback());
        }

        public async Task Authenticate()
        {
            await _authenticationStartSemaphore.WaitAsync();
            if (_spotifyClient != null)
            {
                return;
            }

            if (File.Exists(RefreshTokenFile))
            {
                try
                {
                    var refreshToken = await File.ReadAllTextAsync(RefreshTokenFile);
                    var initialResponse = JsonConvert.DeserializeObject<PKCETokenResponse>(refreshToken);

                    var authenticator = new PKCEAuthenticator(ClientId, initialResponse);
                    var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
                    _spotifyClient = new SpotifyClient(config);
                    return;
                }
                catch (Exception ex)
                {
                    File.Delete(RefreshTokenFile);
                }
            }

            try
            {
                var (verifier, challenge) = PKCEUtil.GenerateCodes();
                _verifier = verifier;

                var loginRequest = new LoginRequest(
                    new Uri("http://localhost:8000/"),
                    ClientId,
                    LoginRequest.ResponseType.Code
                )
                {
                    CodeChallengeMethod = "S256",
                    CodeChallenge = challenge,
                    Scope = new[] { Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
                };
                var uri = loginRequest.ToUri();

#pragma warning disable CS4014
                Task.Run(async () =>
#pragma warning restore CS4014
                {
                    var tcp = new TcpListener(IPAddress.Loopback, 8000);
                    tcp.Start();
                    using var tcpClient = await tcp.AcceptTcpClientAsync();
                    await using var stream = tcpClient.GetStream();
                    using var streamReader = new StreamReader(stream);

                    while (true)
                    {
                        var incoming = await streamReader.ReadLineAsync();

                        var match = _regex.Match(incoming);
                        if (match.Success)
                        {
                            var code = match.Groups["code"].Value;
                            await GetCallback(code);
                            await RespondOk(tcpClient);
                            tcp.Stop();
                            return;
                        }
                    }
                });

                _authenticationInProgressSemaphore?.Dispose();
                _authenticationInProgressSemaphore = new SemaphoreSlim(0, 1);
                Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
                await _authenticationInProgressSemaphore.WaitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _eventAggregator.PublishOnUIThread(new ErrorHappened() { Text = ex.Message });
                throw;
            }
            finally
            {
                _authenticationStartSemaphore.Release();
            }
        }

        private static async Task RespondOk(TcpClient tcpClient)
        {
            await using var writer = new StreamWriter(tcpClient.GetStream());
            await writer.WriteAsync("HTTP/1.0 200 OK");
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync("Content-Type: text/plain; charset=UTF-8");
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync("Content-Length: " + 0);
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync(Environment.NewLine);
            await writer.FlushAsync();
        }

        private async Task GetCallback(string code)
        {
            try
            {
                var initialResponse =
                    await new OAuthClient().RequestToken(new PKCETokenRequest(ClientId, code,
                        new Uri("http://localhost:8000/"), _verifier));

                var authenticator = new PKCEAuthenticator(ClientId, initialResponse);
                await File.WriteAllTextAsync(RefreshTokenFile, JsonConvert.SerializeObject(initialResponse));

                var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
                _spotifyClient = new SpotifyClient(config);
            }
            finally
            {
                _authenticationInProgressSemaphore.Release(1);
            }
        }

        private async Task<T> TrySpotify<T>(Func<Task<T>> spotifyAction)
        {
            try
            {
                if (_spotifyClient == null)
                {
                    await Authenticate();
                }

                return await spotifyAction.Invoke();
            }
            catch (APIUnauthorizedException)
            {
                try
                {
                    _spotifyClient = null;
                    await Authenticate();
                    return await spotifyAction.Invoke();
                }
                catch (Exception ex)
                {
                    _eventAggregator.PublishOnUIThread(new ErrorHappened { Text = ex.Message });
                    throw;
                }
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(new ErrorHappened { Text = ex.Message });
                throw;
            }
        }
    }
}