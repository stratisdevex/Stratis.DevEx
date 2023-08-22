namespace Cobra.Api.Node.Cirrus.Models;

using Newtonsoft.Json;

public class WalletAddress
{
    [JsonProperty("address", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string Address { get; set; } = string.Empty;

    [JsonProperty("isUsed", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public bool IsUsed { get; set; }

    [JsonProperty("isChange", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public bool IsChange { get; set; }
}
