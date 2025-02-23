using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum;

namespace Stratis.DevEx.Ethereum
{
    public static class Extensions
    {
        public static bool IsNetworkError<T>(this Result<T> result)
        {
            if (result.Exception is not null && result.Exception.GetType().Name.Contains("RpcClient"))
            {
                return true;
            }
            else
            {
                return false;   
            }
        }
    }
}
