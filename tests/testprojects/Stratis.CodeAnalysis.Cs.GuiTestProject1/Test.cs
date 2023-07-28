using Stratis.SmartContracts;

//namespace foo
//{
    public class Test : SmartContract
    {
        
        public Test(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
            
            byte[] b = { };
            var kk = State.GetUInt32(nameof(PingsSent));
            Assert(kk > 10, "foo");
            //b.Clone();
            //var v = NLog.Common.InternalLogger.IncludeTimestamp;
        }

        public void Foo()
        {
            byte[] b = { };
            var kk = State.GetUInt32(nameof(PingsSent));
            
            if (kk>3) {
                var g = PingsSent;
            } 
            else

            {
                var hh = PingsSent2;
                Assert(hh > 5, "PingsSent2 must be more than 5");
            }
            
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


        public uint PingsSent3
        {
            get => State.GetUInt32(nameof(PingsSent));
            private set => State.SetUInt32(nameof(PingsSent), value);
        }



    }