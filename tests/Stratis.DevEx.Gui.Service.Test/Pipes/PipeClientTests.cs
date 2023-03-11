using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Stratis.DevEx.Pipes;
using Stratis.DevEx.Pipes.Formatters;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Stratis.DevEx.Gui.Service.Test.Pipes
{
    [TestClass]
    public class PipeClientTests
    {
        [TestMethod]
        public async Task ConnectTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await using var client = new PipeClient<string>("this_pipe_100%_is_not_exists");

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await client.ConnectAsync(cancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task WriteAsyncTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var cancellationToken = cancellationTokenSource.Token;

            await using var client = new PipeClient<string>("this_pipe_100%_is_not_exists");

            var firstTask = Task.Run(async () =>
            {
                using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                source.CancelAfter(TimeSpan.FromSeconds(1));

            // ReSharper disable once AccessToDisposedClosure
            await client.WriteAsync(string.Empty, source.Token);
            }, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            {
                using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                source.CancelAfter(TimeSpan.FromSeconds(1));

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    async () => await client.WriteAsync(string.Empty, source.Token));
            }

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await firstTask);
        }
    }
}