using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Stratis.DevEx.Ethereum.Explorers;

namespace Stratis.DevEx.Ethereum.Test
{
    public class BlockscoutExplorerTests
    {
        [Fact]
        public async Task CanCall()
        {
            var c = new BlockscoutClient(new HttpClient());
            var t = await c.Get_statsAsync();
            Assert.NotNull(t);
        }
    }
}
