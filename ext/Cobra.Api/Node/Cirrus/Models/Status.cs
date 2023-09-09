using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cobra.Api.Node.Cirrus.Models
{
    [ReadOnly(true)]
    public partial class Status
    {
        [Category("Node Server")] [JsonProperty("agent")] public string Agent { get; set; } = string.Empty;

        [Category("Node Server")] [JsonProperty("version")] public string Version { get; set; } = string.Empty;

        [Category("Node")] [JsonProperty("externalAddress")] public string ExternalAddress { get; set; } = string.Empty;

        [Category("Node")] [JsonProperty("network")] public string Network { get; set; } = string.Empty;

        [Category("Node")] [JsonProperty("coinTicker")] public string CoinTicker { get; set; } = string.Empty;

        [JsonProperty("processId")]
        [Category("Node Server")] public long ProcessId { get; set; }

        [Category("Node")] [JsonProperty("consensusHeight")]
        public long ConsensusHeight { get; set; }

        [Category("Node")] [JsonProperty("headerHeight")]
        public long HeaderHeight { get; set; }

        [Category("Node")] [JsonProperty("blockStoreHeight")]
        public long BlockStoreHeight { get; set; }

        [Category("Node")] [JsonProperty("bestPeerHeight")]
        public long BestPeerHeight { get; set; }

        [JsonProperty("inboundPeers")]
        public object[] InboundPeers { get; set; } = Array.Empty<object>();

        [JsonProperty("outboundPeers")]
        public OutboundPeer[] OutboundPeers { get; set; } = Array.Empty<OutboundPeer>();

        [JsonProperty("featuresData")]
        public FeaturesDatum[] FeaturesData { get; set; } = Array.Empty<FeaturesDatum>();

        [Category("Node Server")][JsonProperty("dataDirectoryPath")] public string DataDirectoryPath { get; set; } = string.Empty;

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
        [Browsable(false)] [JsonProperty("namespace")] public string Namespace { get; set; } = string.Empty;

        [Browsable(false)] [JsonProperty("state")] public string State { get; set; } = string.Empty;

        [JsonIgnore]
        public string Description => Namespace + " - " + State; 
    }

    public partial class OutboundPeer
    {
        [JsonProperty("version")] public string Version { get; set; } = string.Empty;

        [JsonProperty("remoteSocketEndpoint")] public string RemoteSocketEndpoint { get; set; } = string.Empty;

        [JsonProperty("tipHeight")]
        public long TipHeight { get; set; }
    }

    public class StatusPropertyObjectConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {

            if (destinationType is not null && destinationType == typeof(string))
            {
                return true;
            }
            else return base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is FeaturesDatum fd)
                return fd.State + " = " + fd.Namespace;
            else return base.ConvertTo(context, culture, value, destinationType);
        }
    }

}
