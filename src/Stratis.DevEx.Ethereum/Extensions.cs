using System;

namespace Stratis.DevEx.Ethereum
{
    public static class Extensions
    {
        public static bool IsNetworkError<T>(this Result<T> result)
        {
            if (result.Exception is null)
            {
                return false;
            }
            else
            {
                var n = result.Exception.GetType().Name;
                return n.Contains("RpcClient") || n.Contains(("Http"));  
            }
        }
    }
}
