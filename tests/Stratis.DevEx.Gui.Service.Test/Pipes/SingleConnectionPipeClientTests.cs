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
    public class SingleConnectionPipeClientTests
    {
        [TestMethod]
        public async Task ClientConnectCancellationTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await using var client = new SingleConnectionPipeClient<string>("this_pipe_100%_is_not_exists");

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await client.ConnectAsync(cancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task ClientConnectTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await using var client = new SingleConnectionPipeClient<string>(nameof(ClientConnectTest));
            await using var server = new SingleConnectionPipeServer<string>(nameof(ClientConnectTest));

            await server.StartAsync(cancellationToken: cancellationTokenSource.Token);

            await client.ConnectAsync(cancellationTokenSource.Token);
        }

        [TestMethod]
        public async Task ClientDoubleConnectTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var client = new SingleConnectionPipeClient<string>(nameof(ClientDoubleConnectTest));
            await using var server = new SingleConnectionPipeServer<string>(nameof(ClientDoubleConnectTest));

            await server.StartAsync(cancellationToken: cancellationTokenSource.Token);

            await client.ConnectAsync(cancellationTokenSource.Token);

            await client.DisconnectAsync(cancellationTokenSource.Token);

            await client.ConnectAsync(cancellationTokenSource.Token);
        }
    }
}