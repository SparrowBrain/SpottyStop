using System;
using System.Threading;
using System.Threading.Tasks;
using SpottyStop.Services;
using Xunit;

namespace SpottyStop.UnitTests.Services
{
    public class DelayedActionFactoryTests
    {
        [Theory]
        [AutoMoqData]
        public async Task CreateDelayedAction_CreatesActionWithADelay(
            DelayedActionFactory sut)
        {
            // Arrange
            var delay = 100;
            var invoked = false;
            var action = new Func<Task>(() =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            // Act
            var result = sut.CreateDelayedAction(action, delay, CancellationToken.None);

            // Assert
            Task.Run(result);
            Assert.False(invoked);
            await Task.Delay(delay);
            Assert.True(invoked);
        }

        [Theory]
        [AutoMoqData]
        public async Task CreateDelayedAction_DoesNotInvokeAction_WhenCancellationIsRequested(
            CancellationTokenSource cts,
            DelayedActionFactory sut)
        {
            // Arrange
            cts.Cancel();
            var delay = 0;
            var invoked = false;
            var action = new Func<Task>(() =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            // Act
            var result = sut.CreateDelayedAction(action, delay, cts.Token);

            // Assert
            await Task.Run(result);
            Assert.False(invoked);
        }
    }
}