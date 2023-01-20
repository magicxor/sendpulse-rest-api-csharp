using Newtonsoft.Json;
using SendPulseSdk.Converters;

namespace SendPulseSdk.Models.ViberCampaign;

public class ViberCampaign
{
    [JsonProperty("task_name")]
    public string Name;
        
    [JsonProperty("recipients")]
    public string[] Recipients = Array.Empty<string>();

    [JsonProperty("address_book")]
    public uint AddressBook;

    [JsonProperty("message")]
    public string Message = "";

    [JsonProperty("message_live_time")]
    public uint MessageLiveTime = 60;

    [JsonProperty("sender_id")]
    public uint SenderId;

    [JsonProperty("send_date")]
    [JsonConverter(typeof(ViberDateTimeConverter))]
    public DateTime SendDate = DateTime.Now;

    [JsonProperty("message_type")]
    public ViberCampaignMessageType MessageType = ViberCampaignMessageType.Marketing;

    [JsonProperty("additional")]
    public ViberCampaignAdditional Additional;
}