using static Stratis.DevEx.Result;

namespace Stratis.DevEx.Ethereum.Test
{
    public class NetworkTests
    {
        [Fact]
        public async Task CanGetChainId()
        {
            var c = await Network.GetChainIdAsync("http://127.0.0.1:7545");
            Assert.Equal(4, c);  
        }

        [Fact]
        public async Task CanGetNetworkId()
        {
            var c = await Network.GetNetworkIdAsync("http://127.0.0.1:7545");
            var c2 = await Network.GetNetworkDetailsAsync("https://rpc.stratisevm.com");
            Assert.Equal("c", c);
        }

        [Fact]
        public async Task CanGetManagedAccounts()
        {
            var n = new Network("http://127.0.0.1:7545", 1337);
            var a = await n.GetPredefinedAccountsAsync();

            n = new Network("https://rpc.stratisevm.com", 105105);
            a = await n.GetPredefinedAccountsAsync();
            Assert.NotNull(a);


            Assert.False(Succedeed(await ExecuteAsync(Network.GetChainIdAsync("http://127.0.0.1:754")), out var r));
            Assert.True(r.IsNetworkError());
            
        }

        [Fact]
        public async Task CanGetProtocolversion()
        {
            //var c = await Network.GetNetworkIdAsync("http://127.0.0.1:7545");
            var v = await Network.GetProtocolVersion("http://127.0.0.1:7545");

            Assert.Equal("c", v);

        }

        [Fact]  
        public async Task CanDeployContract()
        {
            const string bytecode = "0x6060604052341561000f57600080fd5b61010d8061001e6000396000f300606060405260043610603f576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff168063165c4a16146044575b600080fd5b3415604e57600080fd5b606b60048080359060200190919080359060200190919050506081565b6040518082815260200191505060405180910390f35b600081830290503373ffffffffffffffffffffffffffffffffffffffff1682847f95f43b7f617d5258b0fb35422a75edbd28be81192e925e2acd477799bb33f4dd846040518082815260200191505060405180910390a4809050929150505600a165627a7a7230582074f731c369f22af1ff268597e914731b26962248a90551127216868b5650c0fb0029";
            var n = new Network("http://127.0.0.1:7545", 1337);
            var r = await n.DeployContract(bytecode, "0xDcca5cDB75965fAf0Fc8905CA0A233ea0C58b489", "0x5075185be243d1c586c79237b1e13271a8e2033b06b93c8d7fc79ad70ae6ea95");
            Assert.NotNull(r);  
        }
    }
}