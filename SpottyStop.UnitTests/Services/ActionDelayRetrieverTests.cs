using System;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Moq;
using SpotifyAPI.Web;
using SpottyStop.Services;
using Xunit;

namespace SpottyStop.UnitTests.Services
{
    public class ActionDelayRetrieverTests
    {
        [Theory]
        [AutoMoqData]
        public async Task GetRemainingSongTimeInMs_ThrowsException_WhenGettingPlaybackFails(
            [Frozen] Mock<ISpotify> spotifyMock,
            ActionDelayRetriever sut)
        {
            // Arrange
            spotifyMock.Setup(x => x.GetPlayback()).Throws(new Exception("bad"));

            // Act
            var act = new Func<Task>(sut.GetRemainingSongTimeInMs);

            // Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(act);
            Assert.Equal("bad", ex.Message);
        }

        [Theory]
        [AutoMoqData]
        public async Task GetRemainingSongTimeInMs_ReturnsRemainingSongTime_WhenGettingPlaybackSucceeds(
            [Frozen] Mock<ISpotify> spotifyMock,
            CurrentlyPlayingContext playbackContext,
            FullTrack track,
            ActionDelayRetriever sut)
        {
            // Arrange
            track.DurationMs = 500;
            playbackContext.Item = track;
            playbackContext.ProgressMs = 200;
            spotifyMock.Setup(x => x.GetPlayback()).ReturnsAsync(playbackContext);

            // Act
            var actual = await sut.GetRemainingSongTimeInMs();

            // Assert
            Assert.Equal(300, actual);
        }
    }
}