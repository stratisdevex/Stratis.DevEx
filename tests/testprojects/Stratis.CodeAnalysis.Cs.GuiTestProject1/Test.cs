using Stratis.SmartContracts;

//namespace foo
//{
    public class Test : SmartContract
    {
        
        public Test(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
            
            //byte[] b = { };
            //b.Clone();
            //var v = NLog.Common.InternalLogger.IncludeTimestamp;
        }

        public uint PingsSent
        {
            get => State.GetUInt32(nameof(PingsSent));
            private set => State.SetUInt32(nameof(PingsSent), value);
        }

        public uint PingsSent2
        {
            get => State.GetUInt32(nameof(PingsSent));
            private set => State.SetUInt32(nameof(PingsSent), value);
        }



    }