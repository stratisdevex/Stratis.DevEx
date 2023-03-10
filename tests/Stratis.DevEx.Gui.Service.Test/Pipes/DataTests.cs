
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Stratis.DevEx.Pipes;
using Stratis.DevEx.Pipes.Formatters;

using Xunit;

namespace Stratis.DevEx.Gui.Service.Test.Pipes
{
    public class DataTests
    {
        [Fact]
        public async Task NullTest()
        {
            var values = new List<string?> { null };
            static string HashFunc(string? value) => value ?? "null";

            await BaseTests.DataSingleTestAsync(values, HashFunc);
            
        }
        
        [Fact]
        public async Task EmptyArrayTest()
        {
            var values = new List<byte[]?> { Array.Empty<byte>() };
            static string HashFunc(byte[]? value) => value?.Length.ToString() ?? "null";

            await BaseTests.DataSingleTestAsync(values, HashFunc);
            //await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter());
            //await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter());
            //await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter());

            values = new List<byte[]?> { null };

            await BaseTests.DataSingleTestAsync(values, HashFunc);
            //await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter());
            //await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter());
            //await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter());
        }

        [Fact]
        public async Task EmptyArrayParallelTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var cancellationToken = cancellationTokenSource.Token;

            const string pipeName = "pipe";
            await using var server = new SingleConnectionPipeServer<string?>(pipeName)
            {
                WaitFreePipe = true
            };
            await using var client = new SingleConnectionPipeClient<string?>(pipeName);

            await server.StartAsync(cancellationToken).ConfigureAwait(false);
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var tasks = Enumerable
                .Range(0, 10000)
                .Select(async _ => await client.WriteAsync(null, cancellationTokenSource.Token))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTests.BinaryDataTestAsync(0);
        }

        [Fact]
        public async Task TestMessageSize1B()
        {
            await BaseTests.BinaryDataTestAsync(1);
        }

        [Fact]
        public async Task TestMessageSize2B()
        {
            await BaseTests.BinaryDataTestAsync(2);
        }

        [Fact]
        public async Task TestMessageSize3B()
        {
            await BaseTests.BinaryDataTestAsync(3);
        }

        [Fact]
        public async Task TestMessageSize9B()
        {
            await BaseTests.BinaryDataTestAsync(9);
        }

        [Fact]
        public async Task TestMessageSize33B()
        {
            await BaseTests.BinaryDataTestAsync(33);
        }

        //[Fact]
        //public async Task TestMessageSize1Kx3_NewtonsoftJson()
        //{
        //    await BaseTests.BinaryDataTestAsync(1025, 3, new NewtonsoftJsonFormatter());
        //}

        //[Fact]
        //public async Task TestMessageSize1Kx3_SystemTextJson()
        //{
        //    await BaseTests.BinaryDataTestAsync(1025, 3, new SystemTextJsonFormatter());
        //}

        //[Fact]
        //public async Task TestMessageSize1Kx3_Ceras()
        //{
        //    await BaseTests.BinaryDataTestAsync(1025, 3, new CerasFormatter());
        //}

        [Fact]
        public async Task TestMessageSize129B()
        {
            await BaseTests.BinaryDataTestAsync(129);
        }

        [Fact]
        public async Task TestMessageSize1K()
        {
            await BaseTests.BinaryDataTestAsync(1025);
        }

        [Fact]
        public async Task TestMessageSize1M()
        {
            await BaseTests.BinaryDataTestAsync(1024 * 1024 + 1);
        }

        [Fact]
        public async Task Single_TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTests.BinaryDataSingleTestAsync(0);
        }

        [Fact]
        public async Task Single_TestMessageSize1B()
        {
            await BaseTests.BinaryDataSingleTestAsync(1);
        }

        //[Fact]
        //public async Task Single_TestMessageSize1Kx3_NewtonsoftJson()
        //{
        //    await BaseTests.BinaryDataSingleTestAsync(1025, 3, new NewtonsoftJsonFormatter());
        //}

        //[Fact]
        //public async Task Single_TestMessageSize1Kx3_SystemTextJson()
        ///{
        //    await BaseTests.BinaryDataSingleTestAsync(1025, 3, new SystemTextJsonFormatter());
        //}

        //[Fact]
        //public async Task Single_TestMessageSize1Kx3_Ceras()
        //{
        //    await BaseTests.BinaryDataSingleTestAsync(1025, 3, new CerasFormatter());
        //}

        [Fact]
        public async Task TypeTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var completionSource = new TaskCompletionSource<bool>(false);
            using var registration = cancellationTokenSource.Token.Register(() => completionSource.TrySetCanceled());

            const string pipeName = "pipe";
            var formatter = new BinaryFormatter();
            await using var server = new PipeServer<object>(pipeName, formatter);
            await using var client = new PipeClient<object>(pipeName, formatter: formatter);

            server.ExceptionOccurred += (_, args) => Assert.Fail(args.Exception.ToString());
            client.ExceptionOccurred += (_, args) => Assert.Fail(args.Exception.ToString());
            server.MessageReceived += (_, args) =>
            {
                Runtime.Info($"MessageReceived: {args.Message as Exception}");

                completionSource.TrySetResult(args.Message is Exception);
            };

            await server.StartAsync(cancellationTokenSource.Token);

            await client.ConnectAsync(cancellationTokenSource.Token);

            await client.WriteAsync(new Exception("Hello. It's server message"), cancellationTokenSource.Token);

            Assert.True(await completionSource.Task);
        }
        
    }
}