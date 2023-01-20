using Newtonsoft.Json.Linq;

namespace SendPulseSdk.Models;

public class SendPulseResponse
{
    public int HttpStatusCode { get; set; }
    public bool IsError { get; set; }
    public JToken Data { get; set; }
    public string SdkErrorMessage { get; set; }
}