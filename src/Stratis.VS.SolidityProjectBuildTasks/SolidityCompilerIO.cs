namespace Stratis.VS.StratisEVM.SolidityCompilerIO
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using CompactJson;
   
    using System.ComponentModel;
   

    public partial class SolidityCompilerInput
    {
        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("sources")]
        public Dictionary<string, Source> Sources { get; set; }

        [JsonProperty("settings")]
        public Settings Settings { get; set; }
    }

    public partial class Settings
    {
        [JsonProperty("remappings")]
        public string[] Remappings { get; set; }

        [JsonProperty("optimizer")]
        public Optimizer Optimizer { get; set; }

        [JsonProperty("evmVersion")]
        public string EvmVersion { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("libraries")]
        public Dictionary<string, string> Libraries { get; set; }

        [JsonProperty("outputSelection")]
        public Dictionary<string, Dictionary<string, string[]>> OutputSelection { get; set; }
    }

    public partial class Metadata
    {
        [JsonProperty("useLiteralContent")]
        public bool UseLiteralContent { get; set; }
    }

    public partial class Optimizer
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("runs")]
        public long Runs { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("keccak256")]
        public string Keccak256 { get; set; }

        [JsonProperty("urls")]
        public string[] Urls { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
