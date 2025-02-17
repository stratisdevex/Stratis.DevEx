using System;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.JsonRpc.Client;
using Nethereum.Web3;

namespace Stratis.DevEx.Ethereum
{
    public class Network : Runtime
    {
        public static async Task<BigInteger> GetChainIdAsync(string rpcurl) => await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();
       
    }
}
