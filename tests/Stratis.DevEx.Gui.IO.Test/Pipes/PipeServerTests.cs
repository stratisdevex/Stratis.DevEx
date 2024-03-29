﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Stratis.DevEx.Pipes;
using Stratis.DevEx.Pipes.Factories;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Stratis.DevEx.Gui.Service.Test.Pipes
{
    [TestClass]
    public class PipeServerTests
    {
        [TestMethod]
        public async Task DisposeTest()
        {
            {
                try
                {
                    using var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
#if !NETFRAMEWORK
                    await using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
#else
                using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
#endif
                }
                catch (OperationCanceledException)
                {
                }
            }

            {
#if !NETFRAMEWORK
                await using var pipe = PipeServerFactory.Create("test");
#else
            using var pipe = PipeServerFactory.Create("test");
#endif
            }
        }

        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1));

            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }
        }

        [TestMethod]
        public async Task DoubleStartWithSameName()
        {
            await using var pipe1 = new PipeServer<string>("test");

            await pipe1.StartAsync();

            await using var pipe2 = new PipeServer<string>("test");

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync());
        }

        [TestMethod]
        public async Task StartStopStart()
        {
            await using var pipe = new PipeServer<string>("test");

            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync();
        }
    }
}