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
        public async Task CanGetManagedAccounts()
        {
            var n = new Network("http://127.0.0.1:7545", 1337);
            var a = await n.GetPredefinedAccountsAsync();

            n = new Network("https://rpc.stratisevm.com", 105105);
            a = await n.GetPredefinedAccountsAsync();
            Assert.NotNull(a);  

            
            if (Succedeed(await ExecuteAsync(n.GetPredefinedAccountsAsync()), out var r))
            {
                Assert.NotNull(r.Value);
            }
        }
    }
}