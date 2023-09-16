using NBitcoin;

namespace Stratis.SmartContracts.TestChain
{
    public class SendCallContractResult
    {
        public ulong Fee { get; set; }
        public string Hex { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public uint256 TransactionId { get; set; } = uint256.Zero;
    }
}
