using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Stratis.DevEx.Pipes;
using Stratis.DevEx.Pipes.Formatters;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Stratis.DevEx.Gui.Service.Test.Pipes
{
    [TestClass]
    public class SingleConnectionPipeServerTests
    {
        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
                await using var pipe = new SingleConnectionPipeServer<string>("test");

                await pipe.StartAsync();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1));

            {
                await using var pipe = new SingleConnectionPipeServer<string>("test");

                await pipe.StartAsync();
            }
        }

        [TestMethod]
        public async Task DoubleStartWithSameName()
        {
            await using var pipe1 = new SingleConnectionPipeServer<string>("test");

            await pipe1.StartAsync();

            await using var pipe2 = new SingleConnectionPipeServer<string>("test");

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync());
        }

        [TestMethod]
        public async Task StartStopStart()
        {
            await using var pipe = new SingleConnectionPipeServer<string>("test");

            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync();
        }
    }
}