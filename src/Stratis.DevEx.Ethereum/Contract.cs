using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.ABI.Model;

using Nethereum.ABI.ABIDeserialisation;

namespace Stratis.DevEx.Ethereum
{
    public class Contract
    {
        public static ContractABI DeserializeABI(string abi) => ABIDeserialiserFactory.DeserialiseContractABI(abi);
        
    }
}
