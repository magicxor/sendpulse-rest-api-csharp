using Newtonsoft.Json;

namespace SendPulseSdk.Models.ViberCampaign;

public class ViberCampaignButton
{
    [JsonProperty("text")]
    public string Text;
    [JsonProperty("link")]
    public string Link;
}