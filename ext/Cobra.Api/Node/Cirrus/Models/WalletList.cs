namespace Cobra.Api.Node.Cirrus.Models;

using Newtonsoft.Json;

public class WalletList
{
    [JsonProperty("walletNames", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string[] WalletNames { get; set; } = Array.Empty<string>();

    [JsonProperty("watchOnlyWallets", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string[] WatchOnlyWallets { get; set; } = Array.Empty<string>();
}
