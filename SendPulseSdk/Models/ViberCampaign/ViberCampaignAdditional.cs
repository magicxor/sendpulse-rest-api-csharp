using Newtonsoft.Json;

namespace SendPulseSdk.Models.ViberCampaign;

public class ViberCampaignAdditional
{
    [JsonProperty("button")]
    public ViberCampaignButton Button;

    [JsonProperty("image")]
    public ViberCampaignImage Image;

    [JsonProperty("resend_sms")]
    public ViberCampaignResendSms ResendSms;
}