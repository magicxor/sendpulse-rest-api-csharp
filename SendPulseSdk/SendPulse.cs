using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendPulseSdk.Models;
using SendPulseSdk.Models.ViberCampaign;

namespace SendPulseSdk;

// Modified official SDK https://github.com/sendpulse/sendpulse-rest-api-csharp
public class SendPulse : ISendPulse
{
    private readonly string _apiurl = "https://api.sendpulse.com";
    private readonly string _userId;
    private readonly string _secret;
    private readonly ILogger<SendPulse> _logger;
    private string _accessToken;
    private int _refreshToken;

    public SendPulse(SendPulseConfig sendPulseConfig, ILogger<SendPulse> logger)
    {
        if (sendPulseConfig.UserId == null || sendPulseConfig.Secret == null)
        {
            logger.LogError("Empty UserId or Secret");
        }

        _userId = sendPulseConfig.UserId;
        _secret = sendPulseConfig.Secret;
        _logger = logger;
        _accessToken = Md5(_userId + "::" + _secret);

        if (_accessToken != null)
        {
            if (!GetToken())
            {
                _logger.LogError("Could not connect to api, check your UserId and Secret");
            }
        }
    }

    public string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Md5(string input)
    {
        using var md5 = MD5.Create();

        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        var sb = new StringBuilder();

        foreach (var hashByte in hashBytes)
        {
            sb.Append(hashByte.ToString("X2"));
        }

        return sb.ToString();
    }

    private JToken ParseJsonString(string jsonString)
    {
        var jsonStringTrimmed = jsonString?.Trim();
        
        if (string.IsNullOrEmpty(jsonStringTrimmed))
        {
            return null;
        }

        try
        {
            var jToken = JToken.Parse(jsonStringTrimmed);

            JToken result = jToken.Type switch
            {
                JTokenType.Object => (JObject)jToken,
                JTokenType.Array => (JArray)jToken,
                _ => null,
            };

            if (result == null)
            {
                _logger.LogWarning(
                    "Invalid JSON response from SendPulse API (jTokenType={jTokenType}): {sendPulseResponse}",
                    jToken.Type, jsonStringTrimmed);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Can't parse JSON response from SendPulse API: {sendPulseResponse}",
                jsonStringTrimmed);
            return null;
        }
    }

    /// <summary>
    /// Form and send request to API service
    /// </summary>
    public SendPulseResponse SendRequest(string path, string method, Dictionary<string, object> data,
        bool useToken = true)
    {
        var originalPath = path;

        string strReturn;
        var response = new SendPulseResponse();
        try
        {
            var stringdata = "";
            if (data != null && data.Count > 0)
                stringdata = MakeRequestString(data);
            method = method.ToUpper();
            if (method == "GET" && stringdata.Length > 0)
            {
                path = path + "?" + stringdata;
            }

            var webReq = (HttpWebRequest)WebRequest.Create(_apiurl + "/" + path);
            webReq.Method = method;
            if (useToken && _accessToken != null)
                webReq.Headers.Add("Authorization", "Bearer " + _accessToken);
            if (method != "GET")
            {
                var buffer = Encoding.ASCII.GetBytes(stringdata);
                webReq.ContentType = "application/x-www-form-urlencoded";
                webReq.ContentLength = buffer.Length;
                var postData = webReq.GetRequestStream();
                postData.Write(buffer, 0, buffer.Length);
                postData.Close();
            }

            try
            {
                var webResp = (HttpWebResponse)webReq.GetResponse();
                var status = webResp.StatusCode;
                response.HttpStatusCode = (int)status;

                var webResponse = webResp.GetResponseStream();
                var streamReader = new StreamReader(webResponse ?? throw new InvalidOperationException("response stream is null"));
                strReturn = streamReader.ReadToEnd();
                response.Data = ParseJsonString(strReturn);
            }
            catch (WebException we)
            {
                var wRespStatusCode = ((HttpWebResponse)we.Response)?.StatusCode;
                if (wRespStatusCode == HttpStatusCode.Unauthorized && _refreshToken == 0)
                {
                    _refreshToken += 1;
                    GetToken();
                    response = SendRequest(originalPath, method, data, useToken);
                }
                else
                {
                    response.HttpStatusCode = (int?)wRespStatusCode ?? 0;
                    var webResponse = ((HttpWebResponse)we.Response)?.GetResponseStream();
                    var streamReader = new StreamReader(webResponse ?? throw new InvalidOperationException("response stream is null") );
                    strReturn = streamReader.ReadToEnd();
                    response.Data = ParseJsonString(strReturn);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
        }
        
        if (response.HttpStatusCode != 200)
        {
            response.IsError = true;
        }

        return response;
    }

    /// <summary>
    /// Make post data string
    /// </summary>
    private string MakeRequestString(Dictionary<string, object> data)
    {
        var stringBuilder = new StringBuilder(64);

        foreach (var item in data)
        {
            if (stringBuilder.Length != 0)
                stringBuilder.Append('&');

            var key = HttpUtility.UrlEncode(item.Key, Encoding.UTF8);

            if (item.Value.GetType().IsArray)
            {
                foreach (var val in (Array)item.Value)
                {
                    stringBuilder.Append(key);
                    stringBuilder.Append("[]=");
                    stringBuilder.Append(HttpUtility.UrlEncode(val.ToString(), Encoding.UTF8));
                }
            }
            else
            {
                stringBuilder.Append(key);
                stringBuilder.Append('=');
                stringBuilder.Append(HttpUtility.UrlEncode(item.Value.ToString(), Encoding.UTF8));
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Sends JSON request to API service.
    /// Supporte HTTP methods is GET and POST.
    /// </summary>
    /// <param name="endpoint">string Endpoint</param>
    /// <param name="method">HttpMethod method</param>
    /// <param name="data">object data</param>
    /// <param name="useToken">bool useToken</param>
    /// <returns>Dictionary</returns>
    /// <exception cref="NotSupportedException">When passed not supported HTTP method</exception>
    public SendPulseResponse SendJsonRequest(string endpoint, HttpMethod method, object data,
        bool useToken = true)
    {
        if (method != HttpMethod.Get && method != HttpMethod.Post)
            throw new NotSupportedException("Method " + method + " not supported yet!");

        var response = new SendPulseResponse();
        string strReturn;

        try
        {
            var json = JsonConvert.SerializeObject(data,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var webRequest = (HttpWebRequest)WebRequest.Create(_apiurl + "/" + endpoint);
            webRequest.Method = method.ToString();

            if (useToken && _accessToken != null)
                webRequest.Headers.Add("Authorization", "Bearer " + _accessToken);

            if (method != HttpMethod.Get)
            {
                var buffer = Encoding.ASCII.GetBytes(json);
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = buffer.Length;
                var postData = webRequest.GetRequestStream();
                postData.Write(buffer, 0, buffer.Length);
                postData.Close();
            }

            try
            {
                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var status = webResponse.StatusCode;
                response.HttpStatusCode = (int)status;

                if (status == HttpStatusCode.Unauthorized && _refreshToken == 0)
                {
                    _refreshToken += 1;
                    GetToken();
                    response = SendJsonRequest(endpoint, method, data);
                }
                else
                {
                    var responseStream = webResponse.GetResponseStream();
                    using (var streamReader = new StreamReader(responseStream ?? throw new InvalidOperationException("response stream is null")))
                    {
                        strReturn = streamReader.ReadToEnd();
                    }

                    response.Data = ParseJsonString(strReturn);
                }
            }
            catch (WebException ex)
            {
                var statusCode = ((HttpWebResponse)ex.Response)?.StatusCode ?? 0;
                response.HttpStatusCode = (int)statusCode;
                var responseStream = ((HttpWebResponse)ex.Response)?.GetResponseStream();
                using (var streamReader = new StreamReader(responseStream ?? throw new InvalidOperationException("response stream is null")))
                {
                    strReturn = streamReader.ReadToEnd();
                }
                response.Data = ParseJsonString(strReturn);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
        }

        if (response.HttpStatusCode != 200)
        {
            response.IsError = true;
        }
        
        return response;
    }

    /// <summary>
    /// Get token and store it
    /// </summary>
    /// <returns>bool</returns>
    private bool GetToken()
    {
        var data = new Dictionary<string, object>();
        data.Add("grant_type", "client_credentials");
        data.Add("client_id", _userId);
        data.Add("client_secret", _secret);
        SendPulseResponse requestResult = null;
        try
        {
            requestResult = SendRequest("oauth/access_token", "POST", data, false);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        if (requestResult == null) return false;
        if (requestResult.HttpStatusCode != 200)
            return false;
        _refreshToken = 0;
        if (requestResult.Data.Type == JTokenType.Object)
        {
            _accessToken = ((JObject)requestResult.Data)["access_token"]?.ToString();
        }

        return true;
    }

    /// <summary>
    /// Process errors
    /// </summary>
    private SendPulseResponse HandleError(string customMessage)
    {
        return new SendPulseResponse
        {
            HttpStatusCode = 0,
            IsError = true,
            Data = null,
            SdkErrorMessage = customMessage,
        };
    }

    /// <summary>
    /// Get list of address books
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public SendPulseResponse ListAddressBooks(int limit, int offset)
    {
        var data = new Dictionary<string, object>();
        if (limit > 0) data.Add("limit", limit);
        if (offset > 0) data.Add("offset", offset);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks", "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get book info
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse GetBookInfo(int id)
    {
        if (id <= 0) return HandleError("Empty book id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + id, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list emails from book
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse GetEmailsFromBook(int id)
    {
        if (id <= 0) return HandleError("Empty book id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + id + "/emails", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove address book
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse RemoveAddressBook(int id)
    {
        if (id <= 0) return HandleError("Empty book id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + id, "DELETE", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Edit address book name
    /// </summary>
    /// <param name="id">String book id</param>
    /// <param name="newname">String book new name</param>
    /// <returns></returns>
    public SendPulseResponse EditAddressBook(int id, string newname)
    {
        if (id <= 0 || newname.Length == 0) return HandleError("Empty new name or book id");
        var data = new Dictionary<string, object>();
        data.Add("name", newname);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + id, "PUT", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Create new address book
    /// </summary>
    /// <param name="bookName"></param>
    /// <returns></returns>
    public SendPulseResponse CreateAddressBook(string bookName)
    {
        if (bookName.Length == 0) return HandleError("Empty book name");
        var data = new Dictionary<string, object>();
        data.Add("bookName", bookName);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Add new emails to book
    /// </summary>
    /// <param name="bookId">int book id</param>
    /// <param name="emails">String A serialized array of emails</param>
    /// <returns></returns>
    public SendPulseResponse AddEmails(int bookId, string emails)
    {
        if (bookId <= 0 || emails.Length == 0) return HandleError("Empty book id or emails");
        var data = new Dictionary<string, object>();
        data.Add("emails", emails);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + bookId + "/emails", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove emails from book
    /// </summary>
    /// <param name="bookId">int book id</param>
    /// <param name="emails">String A serialized array of emails</param>
    /// <returns></returns>
    public SendPulseResponse RemoveEmails(int bookId, string emails)
    {
        if (bookId <= 0 || emails.Length == 0) return HandleError("Empty book id or emails");
        var data = new Dictionary<string, object>();
        data.Add("emails", emails);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + bookId + "/emails", "DELETE", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get information about email from book
    /// </summary>
    /// <param name="bookId"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse GetEmailInfo(int bookId, string email)
    {
        if (bookId <= 0 || email.Length == 0) return HandleError("Empty book id or email");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + bookId + "/emails/" + email, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Calculate cost of the campaign based on address book
    /// </summary>
    /// <param name="bookId"></param>
    /// <returns></returns>
    public SendPulseResponse CampaignCost(int bookId)
    {
        if (bookId <= 0) return HandleError("Empty book id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("addressbooks/" + bookId + "/cost", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of campaigns
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public SendPulseResponse ListCampaigns(int limit, int offset)
    {
        var data = new Dictionary<string, object>();
        if (limit > 0) data.Add("limit", limit);
        if (offset > 0) data.Add("offset", offset);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("campaigns", "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get information about campaign
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse GetCampaignInfo(int id)
    {
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("campaigns/" + id, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get campaign statistic by countries
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse CampaignStatByCountries(int id)
    {
        if (id <= 0) return HandleError("Empty campaign id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("campaigns/" + id + "/countries", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get campaign statistic by referrals
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse CampaignStatByReferrals(int id)
    {
        if (id <= 0) return HandleError("Empty campaign id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("campaigns/" + id + "/referrals", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Creating a campaign
    /// </summary>
    /// <param name="senderName"></param>
    /// <param name="senderEmail"></param>
    /// <param name="subject"></param>
    /// <param name="body"></param>
    /// <param name="bookId"></param>
    /// <param name="name"></param>
    /// <param name="sendDate"></param>
    /// <param name="attachments"></param>
    /// <returns></returns>
    public SendPulseResponse CreateCampaign(string senderName, string senderEmail, string subject, string body,
        int bookId, string name, string sendDate = "", string attachments = "")
    {
        if (senderName.Length == 0 || senderEmail.Length == 0 || subject.Length == 0 || body.Length == 0 || bookId <= 0)
            return HandleError("Not all data.");
        var encodedBody = Base64Encode(body);
        var data = new Dictionary<string, object>();
        if (attachments.Length > 0) data.Add("attachments", attachments);
        if (sendDate.Length > 0) data.Add("send_date", sendDate);
        data.Add("sender_name", senderName);
        data.Add("sender_email", senderEmail);
        data.Add("subject", subject);
        if (encodedBody.Length > 0) data.Add("body", encodedBody);
        data.Add("list_id", bookId);
        if (name.Length > 0) data.Add("name", name);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("campaigns", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Cancel campaign
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse CancelCampaign(int id)
    {
        if (id <= 0) return HandleError("Empty campaign id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("campaigns/" + id, "DELETE", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of allowed senders
    /// </summary>
    /// <returns></returns>
    public SendPulseResponse ListSenders()
    {
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("senders", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Add new sender
    /// </summary>
    /// <param name="senderName"></param>
    /// <param name="senderEmail"></param>
    /// <returns></returns>
    public SendPulseResponse AddSender(string senderName, string senderEmail)
    {
        if (senderName.Length == 0 || senderEmail.Length == 0) return HandleError("Empty sender name or email");
        var data = new Dictionary<string, object>();
        data.Add("name", senderName);
        data.Add("email", senderEmail);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("senders", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove sender
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse RemoveSender(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        var data = new Dictionary<string, object>();
        data.Add("email", email);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("senders", "DELETE", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Activate sender using code from mail
    /// </summary>
    /// <param name="email"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public SendPulseResponse ActivateSender(string email, string code)
    {
        if (email.Length == 0 || code.Length == 0) return HandleError("Empty email or activation code");
        var data = new Dictionary<string, object>();
        data.Add("code", code);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("senders/" + email + "/code", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Send mail with activation code on sender email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse GetSenderActivationMail(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("senders/" + email + "/code", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get global information about email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse GetEmailGlobalInfo(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("emails/" + email, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove email address from all books
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse RemoveEmailFromAllBooks(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("emails/" + email, "DELETE", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get statistic for email by all campaigns
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse EmailStatByCampaigns(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("emails/" + email + "/campaigns", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Show emails from blacklist
    /// </summary>
    /// <returns></returns>
    public SendPulseResponse GetBlackList()
    {
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("blacklist", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Add email address to blacklist
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    public SendPulseResponse AddToBlackList(string emails)
    {
        if (emails.Length == 0) return HandleError("Empty emails");
        var data = new Dictionary<string, object>();
        var encodedemails = Base64Encode(emails);
        data.Add("emails", encodedemails);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("blacklist", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove email address from blacklist
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    public SendPulseResponse RemoveFromBlackList(string emails)
    {
        if (emails.Length == 0) return HandleError("Empty emails");
        var data = new Dictionary<string, object>();
        var encodedemails = Base64Encode(emails);
        data.Add("emails", encodedemails);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("blacklist", "DELETE", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Return user balance
    /// </summary>
    /// <param name="currency"></param>
    /// <returns></returns>
    public SendPulseResponse GetBalance(string currency)
    {
        var url = "balance";
        if (currency.Length > 0)
        {
            currency = currency.ToUpper();
            url = url + "/" + currency;
        }

        SendPulseResponse result = null;
        try
        {
            result = SendRequest(url, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Send mail using SMTP
    /// </summary>
    /// <param name="emaildata"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpSendMail(Dictionary<string, object> emaildata)
    {
        if (emaildata.Count == 0) return HandleError("Empty email data");
        var html = emaildata["html"].ToString();
        emaildata.Remove("html");
        emaildata.Add("html", Base64Encode(html));
        var data = new Dictionary<string, object>();
        var serialized = JsonConvert.SerializeObject(emaildata);
        data.Add("email", serialized);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/emails", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of emails that was sent by SMTP 
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="sender"></param>
    /// <param name="recipient"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpListEmails(int limit, int offset, string fromDate, string toDate,
        string sender, string recipient)
    {
        var data = new Dictionary<string, object>();
        data.Add("limit", limit);
        data.Add("offset", offset);
        if (fromDate.Length > 0) data.Add("from", fromDate);
        if (toDate.Length > 0) data.Add("to", toDate);
        if (sender.Length > 0) data.Add("sender", sender);
        if (recipient.Length > 0) data.Add("recipient", recipient);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/emails", "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get information about email by his id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpGetEmailInfoById(string id)
    {
        if (id.Length == 0) return HandleError("Empty id");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/emails/" + id, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Unsubscribe emails using SMTP
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpUnsubscribeEmails(string emails)
    {
        if (emails.Length == 0) return HandleError("Empty emails");
        var data = new Dictionary<string, object>();
        data.Add("emails", emails);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/smtp/unsubscribe", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove emails from unsubscribe list using SMTP
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpRemoveFromUnsubscribe(string emails)
    {
        if (emails.Length == 0) return HandleError("Empty emails");
        var data = new Dictionary<string, object>();
        data.Add("emails", emails);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/smtp/unsubscribe", "DELETE", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of allowed IPs using SMTP
    /// </summary>
    /// <returns></returns>
    public SendPulseResponse SmtpListIp()
    {
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/ips", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of allowed domains using SMTP
    /// </summary>
    /// <returns></returns>
    public SendPulseResponse SmtpListAllowedDomains()
    {
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/domains", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Add domain using SMTP
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpAddDomain(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        var data = new Dictionary<string, object>();
        data.Add("email", email);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/domains", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Send confirm mail to verify new domain
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public SendPulseResponse SmtpVerifyDomain(string email)
    {
        if (email.Length == 0) return HandleError("Empty email");
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("smtp/domains/" + email, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of push campaigns
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public SendPulseResponse PushListCampaigns(int limit, int offset)
    {
        var data = new Dictionary<string, object>();
        if (limit > 0) data.Add("limit", limit);
        if (offset > 0) data.Add("offset", offset);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("push/tasks", "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get push campaigns info
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse PushCampaignInfo(int id)
    {
        if (id > 0)
        {
            SendPulseResponse result = null;
            try
            {
                result = SendRequest("push/tasks/" + id, "GET", null);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("No such push campaign");
    }

    /// <summary>
    /// Get amount of websites
    /// </summary>
    /// <returns></returns>
    public SendPulseResponse PushCountWebsites()
    {
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("push/websites/total", "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of websites
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public SendPulseResponse PushListWebsites(int limit, int offset)
    {
        var data = new Dictionary<string, object>();
        if (limit > 0) data.Add("limit", limit);
        if (offset > 0) data.Add("offset", offset);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("push/websites", "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get list of all variables for website
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse PushListWebsiteVariables(int id)
    {
        SendPulseResponse result = null;
        string url;
        if (id > 0)
        {
            url = "push/websites/" + id + "/variables";
            try
            {
                result = SendRequest(url, "GET", null);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }
        }

        return result;
    }

    /// <summary>
    /// Get list of subscriptions for the website
    /// </summary>
    /// <param name="id"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public SendPulseResponse PushListWebsiteSubscriptions(int id, int limit, int offset)
    {
        SendPulseResponse result = null;
        string url;
        if (id > 0)
        {
            var data = new Dictionary<string, object>();
            if (limit > 0) data.Add("limit", limit);
            if (offset > 0) data.Add("offset", offset);
            url = "push/websites/" + id + "/subscriptions";
            try
            {
                result = SendRequest(url, "GET", data);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Get amount of subscriptions for the site
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public SendPulseResponse PushCountWebsiteSubscriptions(int id)
    {
        SendPulseResponse result = null;
        string url;
        if (id > 0)
        {
            url = "push/websites/" + id + "/subscriptions/total";
            try
            {
                result = SendRequest(url, "GET", null);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Set state for subscription
    /// </summary>
    /// <param name="id"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public SendPulseResponse PushSetSubscriptionState(int id, int state)
    {
        if (id > 0)
        {
            var data = new Dictionary<string, object>();
            data.Add("id", id);
            data.Add("state", state);
            SendPulseResponse result = null;
            try
            {
                result = SendRequest("push/subscriptions/state", "POST", data);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Create new push campaign
    /// </summary>
    /// <param name="taskinfo"></param>
    /// <param name="additionalParams"></param>
    /// <returns></returns>
    public SendPulseResponse CreatePushTask(Dictionary<string, object> taskinfo,
        Dictionary<string, object> additionalParams)
    {
        var data = taskinfo;
        if (!data.ContainsKey("ttl")) data.Add("ttl", 0);
        if (!data.ContainsKey("title") || !data.ContainsKey("website_id") || !data.ContainsKey("body"))
        {
            return HandleError("Not all data");
        }

        if (additionalParams != null && additionalParams.Count > 0)
        {
            foreach (var item in additionalParams)
            {
                data.Add(item.Key, item.Value);
            }
        }

        SendPulseResponse result = null;
        try
        {
            result = SendRequest("push/tasks", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Add phones to address book.
    /// </summary>
    /// <returns>The phones.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phones">Phones.</param>
    public SendPulseResponse AddPhones(int bookId, string phones)
    {
        if (bookId <= 0 || phones.Length == 0) return HandleError("Empty book id or phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        data.Add("addressBookId", bookId);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/numbers", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove phones from address book.
    /// </summary>
    /// <returns>The phones.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phones">Phones.</param>
    public SendPulseResponse RemovePhones(int bookId, string phones)
    {
        if (bookId <= 0 || phones.Length == 0) return HandleError("Empty book id or phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        data.Add("addressBookId", bookId);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/numbers", "DELETE", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Update phones.
    /// </summary>
    /// <returns>The phones.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phones">Phones.</param>
    /// <param name="variables">Variables.</param>
    public SendPulseResponse UpdatePhones(int bookId, string phones, string variables)
    {
        if (bookId <= 0 || phones.Length == 0) return HandleError("Empty book id or phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        data.Add("variables", variables);
        data.Add("addressBookId", bookId);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/numbers", "PUT", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get the phone number info.
    /// </summary>
    /// <returns>The phone info.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phoneNumber">Phone number.</param>
    public SendPulseResponse GetPhoneInfo(int bookId, string phoneNumber)
    {
        SendPulseResponse result = null;
        string url;
        if (bookId > 0)
        {
            url = "/sms/numbers/info/" + bookId + "/" + phoneNumber;
            try
            {
                result = SendRequest(url, "GET", null);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Add phones to black list.
    /// </summary>
    /// <returns>The phone to black list.</returns>
    /// <param name="phones">Phones.</param>
    /// <param name="description">Description.</param>
    public SendPulseResponse AddPhonesToBlackList(string phones, string description)
    {
        if (phones.Length == 0) return HandleError("Empty phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        if (description != null)
        {
            data.Add("description", description);
        }

        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/black_list", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Remove phones from black list.
    /// </summary>
    /// <returns>The phones from black list.</returns>
    /// <param name="phones">Phones.</param>
    public SendPulseResponse RemovePhonesFromBlackList(string phones)
    {
        if (phones.Length == 0) return HandleError("Empty phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/black_list", "DELETE", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get black list of phone numbers.
    /// </summary>
    /// <returns>The black list phones.</returns>
    public SendPulseResponse GetBlackListPhones()
    {
        SendPulseResponse result = null;
        var url = "/sms/black_list";
        try
        {
            result = SendRequest(url, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Retrieving information of telephone numbers in the blacklist
    /// </summary>
    /// <returns>The phones info in black list.</returns>
    public SendPulseResponse GetPhonesInfoInBlackList(string phones)
    {
        SendPulseResponse result = null;
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        var url = "/sms/black_list/by_numbers";
        try
        {
            result = SendRequest(url, "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }


    /// <summary>
    /// Send the sms campaign.
    /// </summary>
    /// <returns>The sms campaign.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="body">Body.</param>
    /// <param name="transliterate">Transliterate.</param>
    /// <param name="sender">Sender.</param>
    /// <param name="date">Date.</param>
    public SendPulseResponse SendSmsCampaign(int bookId, string body, int transliterate = 1,
        string sender = "", string date = "")
    {
        if (body.Length == 0) return HandleError("Empty Body");
        if (bookId <= 0) return HandleError("Empty address book Id");
        var data = new Dictionary<string, object>();
        data.Add("addressBookId", bookId);
        data.Add("body", body);
        if (sender != null)
        {
            data.Add("sender", sender);
        }

        data.Add("transliterate", transliterate);
        if (date != null)
        {
            data.Add("date", date);
        }

        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/campaigns", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Send sms campaign by phones list.
    /// </summary>
    /// <returns>The sms campaign by phones.</returns>
    /// <param name="phones">Phones.</param>
    /// <param name="body">Body.</param>
    /// <param name="transliterate">Transliterate.</param>
    /// <param name="sender">Sender.</param>
    /// <param name="date">Date.</param>
    public SendPulseResponse SendSmsCampaignByPhones(string phones, string body, int transliterate = 1,
        string sender = "", string date = "")
    {
        if (body.Length == 0) return HandleError("Empty Body");
        if (phones.Length == 0) return HandleError("Empty phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        data.Add("body", body);
        if (sender != null)
        {
            data.Add("sender", sender);
        }

        data.Add("transliterate", transliterate);
        if (date != null)
        {
            data.Add("date", date);
        }

        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/send", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get sms campaigns list.
    /// </summary>
    /// <returns>The sms campaigns list.</returns>
    /// <param name="dateFrom">Date from.</param>
    /// <param name="dateTo">Date to.</param>
    public SendPulseResponse GetSmsCampaignsList(string dateFrom, string dateTo)
    {
        SendPulseResponse result = null;
        var data = new Dictionary<string, object>();
        data.Add("dateFrom", dateFrom);
        data.Add("dateTo", dateTo);
        var url = "/sms/campaigns/list";
        try
        {
            result = SendRequest(url, "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get sms campaign info.
    /// </summary>
    /// <returns>The sms campaign info.</returns>
    /// <param name="id">Identifier.</param>
    public SendPulseResponse GetSmsCampaignInfo(int id)
    {
        SendPulseResponse result = null;
        string url;
        if (id > 0)
        {
            url = "/sms/campaigns/info/" + id;
            try
            {
                result = SendRequest(url, "GET", null);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Cancel sms campaign.
    /// </summary>
    /// <returns>The sms campaign.</returns>
    /// <param name="id">Identifier.</param>
    public SendPulseResponse CancelSmsCampaign(int id)
    {
        SendPulseResponse result = null;
        string url;
        if (id > 0)
        {
            url = "/sms/campaigns/cancel/" + id;
            try
            {
                result = SendRequest(url, "GET", null);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Get sms campaign cost.
    /// </summary>
    /// <returns>The sms campaign cost.</returns>
    /// <param name="body">Body.</param>
    /// <param name="sender">Sender.</param>
    /// <param name="addressBookId">Address book identifier.</param>
    /// <param name="phones">Phones.</param>
    public SendPulseResponse GetSmsCampaignCost(string body, string sender, int addressBookId = 0,
        string phones = "")
    {
        if (body.Length == 0) return HandleError("Empty Body");
        SendPulseResponse result = null;
        var data = new Dictionary<string, object>();
        data.Add("body", body);
        data.Add("sender", sender);
        if (addressBookId <= 0 && phones == null)
        {
            return HandleError("Empty recipients list");
        }

        if (phones.Length > 0)
        {
            data.Add("phones", phones);
        }
        else
        {
            data.Add("addressBookId", addressBookId);
        }

        var url = "/sms/campaigns/cost";
        try
        {
            result = SendRequest(url, "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Delete sms campaign.
    /// </summary>
    /// <returns>The sms campaign.</returns>
    /// <param name="id">Identifier.</param>
    public SendPulseResponse DeleteSmsCampaign(int id)
    {
        SendPulseResponse result = null;
        string url;
        if (id > 0)
        {
            var data = new Dictionary<string, object>();
            data.Add("id", id);
            url = "/sms/campaigns";
            try
            {
                result = SendRequest(url, "DELETE", data);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unexpected error");
            }

            return result;
        }

        return HandleError("Empty ID");
    }

    /// <summary>
    /// Add phones to addreess book.
    /// </summary>
    /// <returns>The phones to addreess book.</returns>
    /// <param name="addressBookId">Address book identifier.</param>
    /// <param name="phones">Phones.</param>
    public SendPulseResponse AddPhonesToAddreessBook(int addressBookId, string phones)
    {
        if (addressBookId <= 0) return HandleError("Empty address book id");
        if (phones.Length == 0) return HandleError("Empty phones");
        var data = new Dictionary<string, object>();
        data.Add("phones", phones);
        data.Add("addressBookId", addressBookId);
        SendPulseResponse result = null;
        try
        {
            result = SendRequest("/sms/numbers/variables", "POST", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Send viber campaign.
    /// </summary>
    /// <param name="viberCampaign">Viber campaign to create</param>
    /// <returns>The viber campaign.</returns>
    public SendPulseResponse SendViberCampaign(ViberCampaign viberCampaign)
    {
        if (viberCampaign.AddressBook == 0 && viberCampaign.Recipients.Length == 0)
            return HandleError("Empty recipients list");

        if (viberCampaign.Message.Length == 0)
            return HandleError("Empty message");

        if (viberCampaign.SenderId == 0)
            return HandleError("Empty sender");

        SendPulseResponse result = null;
        try
        {
            result = SendJsonRequest("/viber", HttpMethod.Post, viberCampaign);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get viber senders list.
    /// </summary>
    /// <returns>The viber senders.</returns>
    public SendPulseResponse GetViberSenders()
    {
        SendPulseResponse result = null;
        var url = "/viber/senders";
        try
        {
            result = SendRequest(url, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get viber tasks list.
    /// </summary>
    /// <returns>The viber tasks list.</returns>
    /// <param name="limit">Limit.</param>
    /// <param name="offset">Offset.</param>
    public SendPulseResponse GetViberTasksList(int limit = 100, int offset = 0)
    {
        SendPulseResponse result = null;
        var data = new Dictionary<string, object>();
        data.Add("limit", limit);
        data.Add("offset", offset);
        var url = "/viber/task";
        try
        {
            result = SendRequest(url, "GET", data);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get viber campaign statistic.
    /// </summary>
    /// <returns>The viber campaign stat.</returns>
    /// <param name="id">Identifier.</param>
    public SendPulseResponse GetViberCampaignStat(int id)
    {
        SendPulseResponse result = null;
        if (id <= 0) return HandleError("Empty id");
        var url = "/viber/task/" + id;
        try
        {
            result = SendRequest(url, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get the viber sender info.
    /// </summary>
    /// <returns>The viber sender.</returns>
    /// <param name="id">Identifier.</param>
    public SendPulseResponse GetViberSender(int id)
    {
        SendPulseResponse result = null;
        if (id <= 0) return HandleError("Empty id");
        var url = "/viber/senders/" + id;
        try
        {
            result = SendRequest(url, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }

    /// <summary>
    /// Get viber task recipients.
    /// </summary>
    /// <returns>The viber task recipients.</returns>
    /// <param name="id">Identifier.</param>
    public SendPulseResponse GetViberTaskRecipients(int id)
    {
        SendPulseResponse result = null;
        if (id <= 0) return HandleError("Empty id");
        var url = "/viber/task/" + id + "/recipients";
        try
        {
            result = SendRequest(url, "GET", null);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unexpected error");
        }

        return result;
    }
}