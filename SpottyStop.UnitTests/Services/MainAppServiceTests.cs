using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Moq;
using SpottyStop.Infrastructure.Events;
using SpottyStop.Services;
using Stylet;
using Xunit;

namespace SpottyStop.UnitTests.Services
{
    public class MainAppServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task QueueShutDown_GetsTimeTillSongEnds(
            [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
            [Frozen] Mock<IActionDelayRetriever> actionDelayRetrieverMock,
            MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    delayAction.Invoke();
                });

            // Act
            await sut.QueueShutDown(CancellationToken.None);

            // Assert
            actionDelayRetrieverMock.Verify(x => x.GetRemainingSongTimeInMs());
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueShutDown_ShutsDownComputer(
            [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
            [Frozen] Mock<IComputer> computerMock,
            MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    mainAction.Invoke();
                });

            // Act
            await sut.QueueShutDown(CancellationToken.None);

            // Assert
            computerMock.Verify(x => x.Shutdown());
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueShutDown_StopsSpotifyPlayback(
            [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
            [Frozen] Mock<ISpotify> spotifyMock,
            MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    mainAction.Invoke();
                });

            // Act
            await sut.QueueShutDown(CancellationToken.None);

            // Assert
            spotifyMock.Verify(x => x.PausePlayback());
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueShutDown_PublishesShutDownAfterSongHappened(
            [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
            [Frozen] Mock<IEventAggregator> eventAggregatorMock,
            MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    mainAction.Invoke();
                });

            // Act
            await sut.QueueShutDown(CancellationToken.None);

            // Assert
            eventAggregatorMock.Verify(x => x.PublishWithDispatcher(It.IsAny<ShutDownAfterSongHappened>(), It.IsAny<Action<Action>>()));
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueStop_GetsTimeTillSongEnds(
           [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
           [Frozen] Mock<IActionDelayRetriever> actionDelayRetrieverMock,
           MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    delayAction.Invoke();
                });

            // Act
            await sut.QueueStop(CancellationToken.None);

            // Assert
            actionDelayRetrieverMock.Verify(x => x.GetRemainingSongTimeInMs());
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueStop_StopsSpotifyPlayback(
            [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
            [Frozen] Mock<ISpotify> spotifyMock,
            MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    mainAction.Invoke();
                });

            // Act
            await sut.QueueStop(CancellationToken.None);

            // Assert
            spotifyMock.Verify(x => x.PausePlayback());
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueStop_PublishesStopAfterSongHappened(
            [Frozen] Mock<IGenericDelayedActionRunner> genericDelayedActionRunnerMock,
            [Frozen] Mock<IEventAggregator> eventAggregatorMock,
            MainAppService sut)
        {
            // Arrange
            genericDelayedActionRunnerMock
                .Setup(x => x.InvokeAfterDelayInParallel(It.IsAny<Func<Task<int>>>(), It.IsAny<Func<Task>>(), CancellationToken.None))
                .Callback<Func<Task<int>>, Func<Task>, CancellationToken>((delayAction, mainAction, ct) =>
                {
                    mainAction.Invoke();
                });

            // Act
            await sut.QueueStop(CancellationToken.None);

            // Assert
            eventAggregatorMock.Verify(x => x.PublishWithDispatcher(It.IsAny<StopAfterSongHappened>(), It.IsAny<Action<Action>>()));
        }
    }
}