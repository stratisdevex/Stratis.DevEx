using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Globalization;

namespace Stratis.VS.StratisEVM.SolidityCompilerIO2
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;


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

    public class SolidityCompilerOutput
    {
        public Dictionary<string, Dictionary<string, ContractStorage>> contracts { get; set; }
        public Error[] errors { get; set; }
        public Dictionary<string, SmartContractId> sources { get; set; }
    }

    public class Contract
    {
        public ContractStorage ContractStorage { get; set; }
    }

    public class ContractStorage
    {
        public Abi[] abi { get; set; }
        public Evm evm { get; set; }
    }

    public class Evm
    {
        public Bytecode bytecode { get; set; }
        public GasEstimates gasEstimates { get; set; }
    }

    public class Bytecode
    {
        public Functiondebugdata functionDebugData { get; set; }
        public object[] generatedSources { get; set; }
        public Linkreferences linkReferences { get; set; }
        [JsonProperty("object")]
        public string _object { get; set; }
        public string opcodes { get; set; }
        public string sourceMap { get; set; }
    }

    public class Functiondebugdata
    {
    }

    public class Linkreferences
    {
    }

    public class SmartContractId
    {
        public int id { get; set; }
    }

    public class Error
    {
        public string component { get; set; }
        public string formattedMessage { get; set; }
        public string message { get; set; }
        public string severity { get; set; }
        public string type { get; set; }
        public string errorCode { get; set; }
        public SourceLocation sourceLocation { get; set; }
    }

    public class SourceLocation
    {
        public int end { get; set; }
        public string file { get; set; }
        public int start { get; set; }
    }

}

public class GasEstimates
{
    public Creation creation { get; set; }
    public External external { get; set; }
}

public class Creation
{
    public string codeDepositCost { get; set; }
    public string executionCost { get; set; }
    public string totalCost { get; set; }
}

public class External
{
    public string basicFunctionuint256 { get; set; }
}

public class Abi
{
    public bool anonymous { get; set; }
    public Input[] inputs { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public object[] outputs { get; set; }
    public string stateMutability { get; set; }
}

public class Input
{
    public bool indexed { get; set; }
    public string internalType { get; set; }
    public string name { get; set; }
    public string type { get; set; }
}

public partial class PackageJsonFile
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
    public string Version { get; set; }

    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string Description { get; set; }

    [JsonProperty("main", NullValueHandling = NullValueHandling.Ignore)]
    public string Main { get; set; }

    [JsonProperty("scripts", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> Scripts { get; set; }

    [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
    public string Author { get; set; }

    [JsonProperty("license", NullValueHandling = NullValueHandling.Ignore)]
    public string License { get; set; }

    [JsonProperty("devDependencies", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> DevDependencies { get; set; }

    [JsonProperty("dependencies", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> Dependencies { get; set; }
}

public partial class PackageJsonFile
{
    public static PackageJsonFile Parse(string json) => JsonConvert.DeserializeObject<PackageJsonFile>(json, Stratis.DevEx.Ethereum.SolidityCompilerIO.PackageJsonConverter.Settings);
    public static string ToJson(PackageJsonFile self) => JsonConvert.SerializeObject(self, Stratis.DevEx.Ethereum.SolidityCompilerIO.PackageJsonConverter.Settings);
}

internal static class PackageJsonConverter
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
    };
}