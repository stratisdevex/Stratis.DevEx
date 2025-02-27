using System;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.JsonRpc.Client;
using Nethereum.Web3;



namespace Stratis.DevEx.Ethereum
{
    public class Network : Runtime
    {
        #region Constructor
        public Network(string rpcUrl, BigInteger chainid)
        {
            if (GetChainIdAsync(rpcUrl).Result != chainid)
            {
                throw new ArgumentException();
            }
            this.rpcUrl = rpcUrl;
            this.chainId = chainid;
            web3 = new Web3(rpcUrl);
        }
        #endregion

        #region Methods
        public static async Task<BigInteger> GetChainIdAsync(string rpcurl) => await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();

        public async Task<BigInteger> GetBlockNoAsync() => await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

        public async Task<string[]> GetPredefinedAccountsAsync() => await web3.Eth.Accounts.SendRequestAsync();

        public async Task<BigInteger> GetBalanceAsync(string acct) => await web3.Eth.GetBalance.SendRequestAsync(acct);
        #endregion

        #region Fields
        public readonly string rpcUrl;
        public readonly BigInteger chainId;
        public readonly Web3 web3;
        #endregion
    }

    public class StratisMainnet : Network
    {
        public StratisMainnet() : base("https://rpc.stratisevm.com", 505505)
        { 
        
        }

        //public const string ExplorerApiUrl = 
    }
}
