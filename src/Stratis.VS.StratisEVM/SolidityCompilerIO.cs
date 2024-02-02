namespace Stratis.VS.StratisEVM.SolidityCompilerIO
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class SolidityCompilerInput
    {
        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("sources")]
        public Sources Sources { get; set; }

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
        public Libraries Libraries { get; set; }

        [JsonProperty("outputSelection")]
        public OutputSelection OutputSelection { get; set; }
    }

    public partial class Libraries
    {
        [JsonProperty("myFile.sol")]
        public LibrariesMyFileSol MyFileSol { get; set; }
    }

    public partial class LibrariesMyFileSol
    {
        [JsonProperty("MyLib")]
        public string MyLib { get; set; }
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

    public partial class OutputSelection
    {
        [JsonProperty("*")]
        public Empty Empty { get; set; }

        [JsonProperty("def")]
        public Def Def { get; set; }
    }

    public partial class Def
    {
        [JsonProperty("MyContract")]
        public string[] MyContract { get; set; }
    }

    public partial class Empty
    {
        [JsonProperty("")]
        public string[] Purple { get; set; }
    }

    public partial class Sources
    {
        [JsonProperty("myFile.sol")]
        public SourcesMyFileSol MyFileSol { get; set; }

        [JsonProperty("mortal")]
        public Mortal Mortal { get; set; }
    }

    public partial class Mortal
    {
        [JsonProperty("keccak256")]
        public string Keccak256 { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public partial class SourcesMyFileSol
    {
        [JsonProperty("keccak256")]
        public string Keccak256 { get; set; }

        [JsonProperty("urls")]
        public string[] Urls { get; set; }
    }
}
