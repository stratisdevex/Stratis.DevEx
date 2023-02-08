using Stratis.SmartContracts;

//namespace foo
//{
    public class Player : SmartContract
    {
        public Player(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
            PlayerAddress = player;
            Opponent = opponent;
            GameName = gameName;
            GameState = (uint)StateType.Provisioned;
            byte[] b = { };
            b.Clone();
            var v = NLog.Common.InternalLogger.IncludeTimestamp;
        }

        public enum StateType : uint
        {
            Provisioned = 0,
            SentPing = 1,
            ReceivedPing = 2,
            Finished = 3
        }

        public uint GameState
        {
            get => State.GetUInt32(nameof(GameState));
            private set => State.SetUInt32(nameof(GameState), value);
        }

        public Address Opponent
        {
            get => State.GetAddress(nameof(Opponent));
            private set => State.SetAddress(nameof(Opponent), value);
        }

        public Address PlayerAddress
        {
            get => State.GetAddress(nameof(PlayerAddress));
            private set => State.SetAddress(nameof(PlayerAddress), value);
        }

        public string GameName
        {
            get => State.GetString(nameof(GameName));
            private set => State.SetString(nameof(GameName), value);
        }

        public uint PingsSent
        {
            get => State.GetUInt32(nameof(PingsSent));
            private set => State.SetUInt32(nameof(PingsSent), value);
        }

        public uint PingsReceived
        {
            get => State.GetUInt32(nameof(PingsReceived));
            private set => State.SetUInt32(nameof(PingsReceived), value);
        }

        public void ReceivePing()
        {
            Assert(Message.Sender == Opponent);
            Assert(GameState == (uint)StateType.SentPing || GameState == (uint)StateType.Provisioned);

            GameState = (uint)StateType.ReceivedPing;

            // We want to overflow the counter here.
            unchecked
            {
                PingsReceived += 1;
            }
        }

        void X(object x, float r)
        {
        //    //try
            //{ }
        }
        public void SendPing()
        {
            //System.Collections.Generic.List<int> l = new System.Collections.Generic.List<int>();
            //float cc = 0f;
            Assert(Message.Sender == PlayerAddress);
            Assert(GameState == (uint)StateType.ReceivedPing || GameState == (uint)StateType.Provisioned);

            var isFinishedResult = Call(Opponent, 0, nameof(Player.IsFinished));

            Assert(isFinishedResult.Success);

            // End the game if the opponent is finished.
            if ((bool)isFinishedResult.ReturnValue)
            {
                GameState = (uint)StateType.Finished;
                return;
            }

            // We want to overflow the counter here.
            unchecked
            {
                PingsSent += 1;
            }

            var callResult = Call(Opponent, 0, nameof(Player.ReceivePing));
            Assert(callResult.Success);
            GameState = (uint)StateType.SentPing;
        }

        public bool IsFinished()
        {
            return GameState == (uint)StateType.Finished;
        }

        public void FinishGame()
        {
            Assert(Message.Sender == PlayerAddress);
            GameState = (uint)StateType.Finished;
        }

        public string xx;
    }

    [Deploy]
    public class Starter : SmartContract
    {
        public Starter(ISmartContractState state)
            : base(state)
        {
        }

        /// <summary>
        /// Creates two contracts that can ping/pong back and forth between each other up to <see cref="maxPingPongTimes"/>.
        /// </summary>
        /// <param name="player1"></param>
        /// <param name="player2"></param>
        /// <param name="gameName"></param>
        /// <param name="maxPingPongTimes"></param>
        public void StartGame(Address player1, Address player2, string gameName)
        {
            var player1CreateResult = Create<Player>(0, new object[] { player1, player2, gameName });

            Assert(player1CreateResult.Success);

            var player2CreateResult = Create<Player>(0, new object[] { player2, player1, gameName });

            Assert(player2CreateResult.Success);

            Log(
                new GameCreated
                {
                    Player1Contract = player1CreateResult.NewContractAddress,
                    Player2Contract = player2CreateResult.NewContractAddress,
                    GameName = gameName
                }
            );

        }

        public struct GameCreated
        {
            [Index]
            public Address Player1Contract;

            [Index]
            public Address Player2Contract;

            public string GameName;
        }
    }
//}