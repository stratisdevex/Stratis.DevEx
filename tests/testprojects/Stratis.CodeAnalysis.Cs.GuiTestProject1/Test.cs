using Stratis.SmartContracts;

//namespace foo
//{
    public class Test : SmartContract
    {
        
        public Test(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
            
            byte[] b = { };
            //b.Clone();
            //var v = NLog.Common.InternalLogger.IncludeTimestamp;
        }

        
    }