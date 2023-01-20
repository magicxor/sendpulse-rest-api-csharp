using Newtonsoft.Json;

namespace SendPulseSdk.Models.ViberCampaign;

public class ViberCampaignResendSms
{
    [JsonProperty("status")]
    public bool Status;

    [JsonProperty("sms_text")]
    public string Text;

    [JsonProperty("sms_sender_name")]
    public string SenderName;
}