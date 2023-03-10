using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Stratis.DevEx;
using Stratis.DevEx.Pipes;
using Stratis.DevEx.Pipes.Formatters;

using Xunit;

namespace Stratis.DevEx.Gui.Service.Test.Pipes
{

    public class BaseTests
    {
        public static async Task DataTestAsync<T>(IPipeServer<T> server, IPipeClient<T> client, List<T> values, Func<T?, string>? hashFunc = null, CancellationToken cancellationToken = default)
        {
            Runtime.Info("Setting up test...");

            var completionSource = new TaskCompletionSource<bool>(false);
            // ReSharper disable once AccessToModifiedClosure
            using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

            var actualHash = (string?)null;
            var clientDisconnected = false;

            server.ClientConnected += (_, _) =>
            {
                Runtime.Info("Client connected");
            };
            server.ClientDisconnected += (_, _) =>
            {
                Runtime.Info("Client disconnected");
                clientDisconnected = true;

            // ReSharper disable once AccessToModifiedClosure
            completionSource.TrySetResult(true);
            };
            server.MessageReceived += (_, args) =>
            {
                Runtime.Info($"Server_OnMessageReceived: {args.Message}");
                actualHash = hashFunc?.Invoke(args.Message);
                Runtime.Info($"ActualHash: {actualHash}");

            // ReSharper disable once AccessToModifiedClosure
            completionSource.TrySetResult(true);
            };
            server.ExceptionOccurred += (_, args) =>
            {
                Runtime.Info($"Server exception occurred: {args.Exception}");

            // ReSharper disable once AccessToModifiedClosure
            completionSource.TrySetException(args.Exception);
            };
            client.Connected += (_, _) => Runtime.Info("Client_OnConnected");
            client.Disconnected += (_, _) => Runtime.Info("Client_OnDisconnected");
            client.MessageReceived += (_, args) => Runtime.Info($"Client_OnMessageReceived: {args.Message}");
            client.ExceptionOccurred += (_, args) =>
            {
                Runtime.Info($"Client exception occurred: {args.Exception}");

            // ReSharper disable once AccessToModifiedClosure
            completionSource.TrySetException(args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetException(exception);
                }
            };

            server.ExceptionOccurred += (_, args) => Runtime.Info(args.Exception.ToString());
            client.ExceptionOccurred += (_, args) => Runtime.Info(args.Exception.ToString());

            await server.StartAsync(cancellationToken).ConfigureAwait(false);
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            Runtime.Info("Client and server started");
            Runtime.Info("---");

            var watcher = Stopwatch.StartNew();

            foreach (var value in values)
            {
                var expectedHash = hashFunc?.Invoke(value);
                Runtime.Info($"ExpectedHash: {expectedHash}");

                await client.WriteAsync(value, cancellationToken).ConfigureAwait(false);

                await completionSource.Task.ConfigureAwait(false);

                if (hashFunc != null)
                {
                    Assert.NotNull(actualHash);
                }

                Assert.Equal(expectedHash, actualHash);
                Assert.False(clientDisconnected, "Server should not disconnect the client for explicitly sending zero-length data");

                Runtime.Info("---");

                completionSource = new TaskCompletionSource<bool>(false);
            }

            Runtime.Info($"Test took {watcher.Elapsed}");
            Runtime.Info("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        public static async Task DataTestAsync<T>(List<T> values, Func<T?, string>? hashFunc = null, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(1));

            const string pipeName = "pipe";
            await using var server = new PipeServer<T>(pipeName, formatter ?? new BinaryFormatter())
            {
#if NET48
            // https://github.com/HavenDV/H.Pipes/issues/6
            WaitFreePipe = true,
#endif
            };
            await using var client = new PipeClient<T>(pipeName, formatter: formatter ?? new BinaryFormatter());

            await DataTestAsync(server, client, values, hashFunc, cancellationTokenSource.Token);
        }

        public static async Task DataSingleTestAsync<T>(List<T> values, Func<T?, string>? hashFunc = null, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(1));

            const string pipeName = "pipe";
            await using var server = new SingleConnectionPipeServer<T>(pipeName, formatter ?? new BinaryFormatter())
            {
                WaitFreePipe = true
            };
            await using var client = new SingleConnectionPipeClient<T>(pipeName, formatter: formatter ?? new BinaryFormatter());

            await DataTestAsync(server, client, values, hashFunc, cancellationTokenSource.Token);
        }

        public static async Task BinaryDataTestAsync(int numBytes, int count = 1, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            await DataTestAsync(GenerateData(numBytes, count), Hash, formatter, timeout);
        }

        public static async Task BinaryDataSingleTestAsync(int numBytes, int count = 1, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            await DataSingleTestAsync(GenerateData(numBytes, count), Hash, formatter, timeout);
        }

        #region Helper methods

        public static List<byte[]> GenerateData(int numBytes, int count = 1)
        {
            var values = new List<byte[]>();

            for (var i = 0; i < count; i++)
            {
                var value = new byte[numBytes];
                new Random().NextBytes(value);

                values.Add(value);
            }

            return values;
        }

        /// <summary>
        /// Computes the SHA-1 hash (lowercase) of the specified byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string Hash(byte[]? bytes)
        {
            if (bytes == null)
            {
                return "null";
            }

            using var sha1 = System.Security.Cryptography.SHA1.Create();

            var hash = sha1.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var @byte in hash)
            {
                sb.Append(@byte.ToString("x2"));
            }
            return sb.ToString();
        }

        #endregion
    }
}
