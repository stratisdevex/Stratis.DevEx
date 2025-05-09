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
            var c2 = await Network.GetChainandNetworkIdAsync("https://rpc.stratisevm.com");
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
    }
}