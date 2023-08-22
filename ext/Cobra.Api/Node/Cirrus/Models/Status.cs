using System;
using System.Collections.Generic;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cobra.Api.Node.Cirrus.Models
{
        public partial class Status
        {
            [JsonProperty("agent")] public string Agent { get; set; } = string.Empty;

            [JsonProperty("version")] public string Version { get; set; } = string.Empty;

            [JsonProperty("externalAddress")] public string ExternalAddress { get; set; } = string.Empty;

            [JsonProperty("network")] public string Network { get; set; } = string.Empty;

            [JsonProperty("coinTicker")] public string CoinTicker { get; set; } = string.Empty;

            [JsonProperty("processId")]
            public long ProcessId { get; set; }

            [JsonProperty("consensusHeight")]
            public long ConsensusHeight { get; set; }

            [JsonProperty("headerHeight")]
            public long HeaderHeight { get; set; }

            [JsonProperty("blockStoreHeight")]
            public long BlockStoreHeight { get; set; }

            [JsonProperty("bestPeerHeight")]
            public long BestPeerHeight { get; set; }

            [JsonProperty("inboundPeers")]
            public object[] InboundPeers { get; set; } = Array.Empty<object>();

            [JsonProperty("outboundPeers")]
            public OutboundPeer[] OutboundPeers { get; set; } = Array.Empty<OutboundPeer>();

            [JsonProperty("featuresData")]
            public FeaturesDatum[] FeaturesData { get; set; } = Array.Empty<FeaturesDatum>();

            [JsonProperty("dataDirectoryPath")] public string DataDirectoryPath { get; set; } = string.Empty;

            [JsonProperty("runningTime")]
            public TimeSpan RunningTime { get; set; }

            [JsonProperty("difficulty")]
            public long Difficulty { get; set; }

            [JsonProperty("protocolVersion")]
            public long ProtocolVersion { get; set; }

            [JsonProperty("testnet")]
            public bool Testnet { get; set; }

            [JsonProperty("relayFee")]
            public double RelayFee { get; set; }

            [JsonProperty("state")] public string State { get; set; } = string.Empty;

            [JsonProperty("inIbd")]
            public bool InIbd { get; set; }
        }

        public partial class FeaturesDatum
        {
            [JsonProperty("namespace")] public string Namespace { get; set; } = string.Empty;

            [JsonProperty("state")] public string State { get; set; } = string.Empty;
        }

        public partial class OutboundPeer
        {
            [JsonProperty("version")] public string Version { get; set; } = string.Empty;

            [JsonProperty("remoteSocketEndpoint")] public string RemoteSocketEndpoint { get; set; } = string.Empty;

            [JsonProperty("tipHeight")]
            public long TipHeight { get; set; }
        }
    


}
