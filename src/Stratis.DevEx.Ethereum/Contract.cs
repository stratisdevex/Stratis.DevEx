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

        public static object[] GetFunctionParamValues(Dictionary<string, (string, object)> contractFunctionParams)
        {
            var output = new object[contractFunctionParams.Count];
            for (int i = 0; i < contractFunctionParams.Count; i++)
            {
                var tv  = contractFunctionParams.ElementAt(i).Value;    
                switch(tv.Item1)
                {
                    case "address":
                        output[i] = tv.Item2.ToString();
                        break;
                    case "uint256":
                    case "uint":
                        output[i] = Convert.ToUInt64(tv.Item2);
                        break;
                    case "int256":
                    case "int":
                        output[i] = Convert.ToInt64(tv.Item2);
                        break;
                    case "string":
                        output[i] = tv.Item2.ToString();
                        break;
                    case "bool":
                        output[i] = Convert.ToBoolean(tv.Item2);
                        break;
                    case "bytes32":
                        output[i] = Convert.ToInt32(tv.Item2);
                        break;
                    case "bytes32[]":
                        output[i] = "[" + (tv.Item2 as IEnumerable<int>).Select(s => "0x" + Convert.ToString(s, 16)).JoinWith(",") + "]";
                        break;
                    default:
                        throw new Exception($"Type {tv.Item1} not supported.");
                }
            }   
            return output;
        }
        
    }
}
