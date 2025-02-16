using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Stratis.DevEx;

namespace Stratis.VS.StratisEVM
{
    public class BlockchainNetworks : Runtime
    {
        public static async Task TestAsync()
        {
            await TaskScheduler.Default;
            var web3 = new Web3("https://mainnet.infura.io/v3/ddd5ed15e8d443e295b696c0d07c8b02");

            // Check the balance of one of the accounts provisioned in our chain, to do that, 
            // we can execute the GetBalance request asynchronously:
            var balance = await web3.Eth.GetBalance.SendRequestAsync("0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae");
        }
    }
}
