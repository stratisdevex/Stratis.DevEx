using System;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.JsonRpc.Client;
using Nethereum.Web3;

using Stratis.DevEx.Ethereum.Explorers;

namespace Stratis.DevEx.Ethereum
{
    public class Network : Runtime
    {
        #region Constructor
        public Network(string rpcUrl, BigInteger chainid)
        {
            this.rpcUrl = rpcUrl;
            this.chainId = chainid;
            web3 = new Web3(rpcUrl);
        }
        #endregion

        #region Methods
        public async Task<BigInteger> GetBlockNoAsync() => await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

        public async Task<string[]> GetPredefinedAccountsAsync() => await web3.Eth.Accounts.SendRequestAsync();

        public async Task<BigInteger> GetBalanceAsync(string acct) => await web3.Eth.GetBalance.SendRequestAsync(acct);

        public static async Task<BigInteger> GetChainIdAsync(string rpcurl) => await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();

        public static async Task<string> GetNetworkIdAsync(string rpcurl) => await new Web3(rpcurl).Net.Version.SendRequestAsync();

        public static async Task<(BigInteger, string)> GetChainandNetworkIdAsync(string rpcurl)
        {
            var web3 = new Web3(rpcurl);
            return (await web3.Eth.ChainId.SendRequestAsync(), await web3.Net.Version.SendRequestAsync());
        }
        #endregion

        #region Fields
        public readonly string rpcUrl;
        public readonly BigInteger chainId;
        public readonly Web3 web3;
        #endregion
    }

    public class StratisMainnet : Network
    {
        public StratisMainnet() : base("https://rpc.stratisevm.com", 105105) {}

        protected BlockscoutClient blockscout = new BlockscoutClient(new System.Net.Http.HttpClient());

        public async Task<StatsResponse> GetExplorerStatsAsync() => await blockscout.Get_statsAsync();
    }
}
