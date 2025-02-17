namespace Stratis.DevEx.Ethereum.Test
{
    public class NetworkTests
    {
        [Fact]
        public async Task CanGetChainId()
        {
            var c = await Network.GetChainId("http://127.0.0.1:7545");
            Assert.Equal(4, c);  
        }
    }
}