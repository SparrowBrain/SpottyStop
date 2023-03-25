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
    public class GenericDelayedActionRunnerTests
    {
        [Theory]
        [AutoMoqData]
        public async Task QueueActionAfterDelay_PublishesError_WhenGettingPlaybackFails(
           [Frozen] Mock<IEventAggregator> eventAggregatorMock,
           Func<Task> mainAction,
           GenericDelayedActionRunner sut)
        {
            // Arrange
            Func<Task<int>> getDelayAction = () => throw new Exception("bad");

            // Act
            await sut.InvokeAfterDelayInParallel(getDelayAction, mainAction, CancellationToken.None);

            // Assert
            eventAggregatorMock.Verify(x => x.PublishWithDispatcher(It.IsAny<ErrorHappened>(), It.IsAny<Action<Action>>()));
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueActionAfterDelay_InvokesMainAction_WithDelayedAction(
            [Frozen] Mock<IDelayedActionFactory> delayedActionFactoryMock,
            Func<Task<int>> getDelayAction,
            GenericDelayedActionRunner sut)
        {
            // Arrange
            var mainActionInvoked = false;
            Func<Task> mainAction = () =>
            {
                mainActionInvoked = true;
                return Task.CompletedTask;
            };
            var semaphoreSlim = new SemaphoreSlim(0);
            delayedActionFactoryMock
                .Setup(x => x.CreateDelayedAction(It.IsAny<Func<Task>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<Func<Task>, int, CancellationToken>((action, delay, token) =>
                {
                    return async () =>
                    {
                        await action.Invoke();
                        semaphoreSlim.Release();
                    };
                });

            // Act
            await sut.InvokeAfterDelayInParallel(getDelayAction, mainAction, CancellationToken.None);

            // Assert
            var delayedActionCalled = await semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(delayedActionCalled);
            Assert.True(mainActionInvoked);
        }
    }
}