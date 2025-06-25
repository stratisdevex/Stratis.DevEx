using System;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.Hex.HexTypes;   
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;   
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

        public static async Task<TransactionReceipt> DeployContract(string rpcurl, string bytecode, string account, string password = null, string abi = null, HexBigInteger gasDeploy = default)
        {
            var web3 = new Web3(rpcurl);

            if (gasDeploy == null)
            {
                gasDeploy = await web3.Eth.DeployContract.EstimateGasAsync(abi, bytecode, account);
            }
         
            if (!await web3.Personal.UnlockAccount.SendRequestAsync(account, "", new HexBigInteger(30)))
            {
                throw new Exception("Could not unlock account using provided password.");
            }
          
            if (abi == null)
            {
                return await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(bytecode, account, gasDeploy);
            }
            else
            {
               
                return await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, bytecode, account, gasDeploy);
            }
        }

        public static async Task<string> GetProtocolVersion(string rpcurl) => await new Web3(rpcurl).Eth.ProtocolVersion.SendRequestAsync();
        
        public static async Task<BigInteger> GetChainIdAsync(string rpcurl) => await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();

        public static async Task<string> GetNetworkIdAsync(string rpcurl) => await new Web3(rpcurl).Net.Version.SendRequestAsync();

        public static async Task<(BigInteger, string, string[])> GetNetworkDetailsAsync(string rpcurl)
        {
            var web3 = new Web3(rpcurl);
            return (await web3.Eth.ChainId.SendRequestAsync(), await web3.Net.Version.SendRequestAsync(), await web3.Eth.Accounts.SendRequestAsync());
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
