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
            const string bytecode2 = "6080604052348015600e575f5ffd5b506101298061001c5f395ff3fe6080604052348015600e575f5ffd5b50600436106030575f3560e01c806360fe47b11460345780636d4ce63c14604c575b5f5ffd5b604a60048036038101906046919060a9565b6066565b005b6052606f565b604051605d919060dc565b60405180910390f35b805f8190555050565b5f5f54905090565b5f5ffd5b5f819050919050565b608b81607b565b81146094575f5ffd5b50565b5f8135905060a3816084565b92915050565b5f6020828403121560bb5760ba6077565b5b5f60c6848285016097565b91505092915050565b60d681607b565b82525050565b5f60208201905060ed5f83018460cf565b9291505056fea264697066735822122095c4e6227cfe49082f59b0543b973bdf9b6bbd91cca6f93a8b707049dc9d33bf64736f6c634300081b0033";
            var r = await Network.DeployContract("http://127.0.0.1:7545", bytecode, "0xFa08D6bB593e556281f88AEa8b836f5642821fd3", "");
            Assert.NotNull(r);  
            Assert.NotNull(r.ContractAddress);  
            r = await Network.DeployContract("http://127.0.0.1:7545", bytecode2, "0xDcca5cDB75965fAf0Fc8905CA0A233ea0C58b489");
            Assert.NotNull(r);
            Assert.NotNull(r.ContractAddress);

        }


        [Fact]
        public async Task CanGetContract()
        {
            const string bytecode = "0x6060604052341561000f57600080fd5b61010d8061001e6000396000f300606060405260043610603f576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff168063165c4a16146044575b600080fd5b3415604e57600080fd5b606b60048080359060200190919080359060200190919050506081565b6040518082815260200191505060405180910390f35b600081830290503373ffffffffffffffffffffffffffffffffffffffff1682847f95f43b7f617d5258b0fb35422a75edbd28be81192e925e2acd477799bb33f4dd846040518082815260200191505060405180910390a4809050929150505600a165627a7a7230582074f731c369f22af1ff268597e914731b26962248a90551127216868b5650c0fb0029";
            //const string bytecode2 = "6080604052348015600e575f5ffd5b506101298061001c5f395ff3fe6080604052348015600e575f5ffd5b50600436106030575f3560e01c806360fe47b11460345780636d4ce63c14604c575b5f5ffd5b604a60048036038101906046919060a9565b6066565b005b6052606f565b604051605d919060dc565b60405180910390f35b805f8190555050565b5f5f54905090565b5f5ffd5b5f819050919050565b608b81607b565b81146094575f5ffd5b50565b5f8135905060a3816084565b92915050565b5f6020828403121560bb5760ba6077565b5b5f60c6848285016097565b91505092915050565b60d681607b565b82525050565b5f60208201905060ed5f83018460cf565b9291505056fea264697066735822122095c4e6227cfe49082f59b0543b973bdf9b6bbd91cca6f93a8b707049dc9d33bf64736f6c634300081b0033";
            var r = await Network.DeployContract("http://127.0.0.1:7545", bytecode, "0xFa08D6bB593e556281f88AEa8b836f5642821fd3", "");
            Assert.NotNull(r);
            Assert.NotNull(r.ContractAddress);
            var c = Network.GetContract("http://127.0.0.1:7545", r.ContractAddress, "");
            Assert.NotNull(c);
        }
    }
}