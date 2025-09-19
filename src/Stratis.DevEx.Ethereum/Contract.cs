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

        public static object[] ParseFunctionParameterValues(Dictionary<string, (string, string)> p) => p.Values.Select(tv => ConvertToType(tv.Item1, tv.Item2)).ToArray();

        // Helper method to convert string to the appropriate .NET type based on Solidity type
        private static object ConvertToType(string solidityType, string value)
        {
            switch (solidityType)
            {
                case "address":
                case "string":
                    return value;
                case "bool":
                    if (bool.TryParse(value, out var b)) return b;
                    if (value == "1") return true;
                    if (value == "0") return false;
                    throw new FormatException($"Cannot convert '{value}' to bool.");
                case "uint":
                case "uint256":
                    if (ulong.TryParse(value, out var ul)) return ul;
                    throw new FormatException($"Cannot convert '{value}' to uint256.");
                case "int":
                case "int256":
                    if (long.TryParse(value, out var l)) return l;
                    throw new FormatException($"Cannot convert '{value}' to int256.");
                case "bytes32":
                    // Accept hex string or convert string to bytes32 (padded/truncated)
                    if (value.StartsWith("0x") && value.Length == 66)
                        return value;
                    // Otherwise, pad/truncate to 32 bytes
                    var bytes = Encoding.UTF8.GetBytes(value);
                    Array.Resize(ref bytes, 32);
                    return bytes;
                case var arr when arr.EndsWith("[]"):
                    // Handle array types
                    var elementType = arr.Substring(0, arr.Length - 2);
                    return value.TrimStart('[').TrimEnd(']').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Select(s => ConvertToType(elementType, s))
                                     .ToArray();
                default:

                    throw new NotSupportedException($"Solidity type '{solidityType}' is not supported.");
            }
        }

        private static object[] ParseCommaDelimitedArray(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<object>();

            var items = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Select(s =>
                             {
                                 // Try to parse as int
                                 if (int.TryParse(s, out int i))
                                     return (object)i;
                                 // Try to parse as double
                                 if (double.TryParse(s, out double d))
                                     return (object)d;
                                 // Try to parse as bool
                                 if (bool.TryParse(s, out bool b))
                                     return (object)b;
                                 // Otherwise, treat as string
                                 return (object)s;
                             })
                             .ToArray();
            return items;
        }

    }
}
