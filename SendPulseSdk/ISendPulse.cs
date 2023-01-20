using SendPulseSdk.Models;
using SendPulseSdk.Models.ViberCampaign;

namespace SendPulseSdk;

public interface ISendPulse
{
    /// <summary>
    /// Get list of address books
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    SendPulseResponse ListAddressBooks(int limit, int offset);
    /// <summary>
    /// Get book info
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse GetBookInfo(int id);
    /// <summary>
    /// Get list pf emails from book
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse GetEmailsFromBook(int id);
    /// <summary>
    /// Remove address book
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse RemoveAddressBook(int id);
    /// <summary>
    /// Edit address book name
    /// </summary>
    /// <param name="id">String book id</param>
    /// <param name="newname">String book new name</param>
    /// <returns></returns>
    SendPulseResponse EditAddressBook(int id, string newname);
    /// <summary>
    /// Create new address book
    /// </summary>
    /// <param name="bookName"></param>
    /// <returns></returns>
    SendPulseResponse CreateAddressBook(string bookName);
    /// <summary>
    /// Add new emails to book
    /// </summary>
    /// <param name="bookId">int book id</param>
    /// <param name="emails">A serialized array of emails</param>
    /// <returns></returns>
    SendPulseResponse AddEmails(int bookId, string emails);
    /// <summary>
    /// Remove emails from book
    /// </summary>
    /// <param name="bookId">int book id</param>
    /// <param name="emails">String A serialized array of emails</param>
    /// <returns></returns>
    SendPulseResponse RemoveEmails(int bookId, string emails);
    /// <summary>
    /// Get information about email from book
    /// </summary>
    /// <param name="bookId"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse GetEmailInfo(int bookId, string email);
    /// <summary>
    /// Calculate cost of the campaign based on address book
    /// </summary>
    /// <param name="bookId"></param>
    /// <returns></returns>
    SendPulseResponse CampaignCost(int bookId);
    /// <summary>
    /// Get list of campaigns
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    SendPulseResponse ListCampaigns(int limit, int offset);
    /// <summary>
    /// Get information about campaign
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse GetCampaignInfo(int id);
    /// <summary>
    /// Get campaign statistic by countries
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse CampaignStatByCountries(int id);
    /// <summary>
    /// Get campaign statistic by referrals
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse CampaignStatByReferrals(int id);
    /// <summary>
    /// Create new campaign
    /// </summary>
    SendPulseResponse CreateCampaign(string senderName, string senderEmail, string subject, string body, int bookId, string name, string sendDate = "", string attachments = "");
    /// <summary>
    /// Cancel campaign
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse CancelCampaign(int id);
    /// <summary>
    /// Get list of allowed senders
    /// </summary>
    /// <returns></returns>
    SendPulseResponse ListSenders();
    /// <summary>
    /// Add new sender
    /// </summary>
    /// <param name="senderName"></param>
    /// <param name="senderEmail"></param>
    /// <returns></returns>
    SendPulseResponse AddSender(string senderName, string senderEmail);
    /// <summary>
    /// Remove sender
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse RemoveSender(string email);
    /// <summary>
    /// Activate sender using code from mail
    /// </summary>
    /// <param name="email"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    SendPulseResponse ActivateSender(string email, string code);
    /// <summary>
    /// Send mail with activation code on sender email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse GetSenderActivationMail(string email);
    /// <summary>
    /// Get global information about email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse GetEmailGlobalInfo(string email);
    /// <summary>
    /// Remove email address from all books
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse RemoveEmailFromAllBooks(string email);
    /// <summary>
    /// Get statistic for email by all campaigns
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse EmailStatByCampaigns(string email);
    /// <summary>
    /// Show emails from blacklist
    /// </summary>
    /// <returns></returns>
    SendPulseResponse GetBlackList();
    /// <summary>
    /// Add email address to blacklist
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    SendPulseResponse AddToBlackList(string emails);
    /// <summary>
    /// Remove email address from blacklist
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    SendPulseResponse RemoveFromBlackList(string emails);
    /// <summary>
    /// Return user balance
    /// </summary>
    /// <param name="currency"></param>
    /// <returns></returns>
    SendPulseResponse GetBalance(string currency);

    /// <summary>
    /// Send mail using SMTP
    /// </summary>
    /// <param name="emaildata"></param>
    /// <returns></returns>
    SendPulseResponse SmtpSendMail(Dictionary<string, object> emaildata);
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
    SendPulseResponse SmtpListEmails(int limit, int offset, string fromDate, string toDate, string sender, string recipient);
    /// <summary>
    /// Get information about email by his id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse SmtpGetEmailInfoById(string id);
    /// <summary>
    /// Unsubscribe emails using SMTP
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    SendPulseResponse SmtpUnsubscribeEmails(string emails);
    /// <summary>
    /// Remove emails from unsubscribe list using SMTP
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    SendPulseResponse SmtpRemoveFromUnsubscribe(string emails);
    /// <summary>
    /// Get list of allowed IPs using SMTP
    /// </summary>
    /// <returns></returns>
    SendPulseResponse SmtpListIp();
    /// <summary>
    /// Get list of allowed domains using SMTP
    /// </summary>
    /// <returns></returns>
    SendPulseResponse SmtpListAllowedDomains();
    /// <summary>
    /// Add domain using SMTP
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse SmtpAddDomain(string email);
    /// <summary>
    /// Send confirm mail to verify new domain
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    SendPulseResponse SmtpVerifyDomain(string email);
    /// <summary>
    /// Get list of push campaigns
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    SendPulseResponse PushListCampaigns(int limit, int offset);
    /// <summary>
    /// Get push campaigns info
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse PushCampaignInfo(int id);
    /// <summary>
    /// Get amount of websites
    /// </summary>
    /// <returns></returns>
    SendPulseResponse PushCountWebsites();
    /// <summary>
    /// Get list of websites
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    SendPulseResponse PushListWebsites(int limit, int offset);
    /// <summary>
    /// Get list of all variables for website
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse PushListWebsiteVariables(int id);
    /// <summary>
    /// Get list of subscriptions for the website
    /// </summary>
    /// <param name="id"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    SendPulseResponse PushListWebsiteSubscriptions(int id, int limit, int offset);
    /// <summary>
    /// Get amount of subscriptions for the site
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    SendPulseResponse PushCountWebsiteSubscriptions(int id);
    /// <summary>
    /// Set state for subscription
    /// </summary>
    /// <param name="id"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    SendPulseResponse PushSetSubscriptionState(int id, int state);
    /// <summary>
    /// Create new push campaign
    /// </summary>
    /// <param name="taskinfo"></param>
    /// <param name="additionalParams"></param>
    /// <returns></returns>
    SendPulseResponse CreatePushTask(Dictionary<string, object> taskinfo, Dictionary<string, object> additionalParams);
    /// <summary>
    /// Add phones to address book.
    /// </summary>
    /// <returns>The phones.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phones">Phones.</param>
    SendPulseResponse AddPhones(int bookId, string phones);
    /// <summary>
    /// Remove phones from address book.
    /// </summary>
    /// <returns>The phones.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phones">Phones.</param>
    SendPulseResponse RemovePhones(int bookId, string phones);
    /// <summary>
    /// Update phones.
    /// </summary>
    /// <returns>The phones.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phones">Phones.</param>
    /// <param name="variables">Variables.</param>
    SendPulseResponse UpdatePhones(int bookId, string phones, string variables);
    /// <summary>
    /// Get the phone number info.
    /// </summary>
    /// <returns>The phone info.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="phoneNumber">Phone number.</param>
    SendPulseResponse GetPhoneInfo(int bookId, string phoneNumber);
    /// <summary>
    /// Add phones to black list.
    /// </summary>
    /// <returns>The phone to black list.</returns>
    /// <param name="phones">Phones.</param>
    /// <param name="description">Description.</param>
    SendPulseResponse AddPhonesToBlackList(string phones, string description);
    /// <summary>
    /// Remove phones from black list.
    /// </summary>
    /// <returns>The phones from black list.</returns>
    /// <param name="phones">Phones.</param>
    SendPulseResponse RemovePhonesFromBlackList(string phones);
    /// <summary>
    /// Get black list of phone numbers.
    /// </summary>
    /// <returns>The black list phones.</returns>
    SendPulseResponse GetBlackListPhones();
    /// <summary>
    /// Retrieving information of telephone numbers in the blacklist
    /// </summary>
    /// <returns>The phones info in black list.</returns>
    SendPulseResponse GetPhonesInfoInBlackList(string phones);
    /// <summary>
    /// Send the sms campaign.
    /// </summary>
    /// <returns>The sms campaign.</returns>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="body">Body.</param>
    /// <param name="transliterate">Transliterate.</param>
    /// <param name="sender">Sender.</param>
    /// <param name="date">Date.</param>
    SendPulseResponse SendSmsCampaign(int bookId, string body, int transliterate = 1, string sender = "", string date = "");
    /// <summary>
    /// Send sms campaign by phones list.
    /// </summary>
    /// <returns>The sms campaign by phones.</returns>
    /// <param name="phones">Phones.</param>
    /// <param name="body">Body.</param>
    /// <param name="transliterate">Transliterate.</param>
    /// <param name="sender">Sender.</param>
    /// <param name="date">Date.</param>
    SendPulseResponse SendSmsCampaignByPhones(string phones, string body, int transliterate = 1, string sender = "", string date = "");
    /// <summary>
    /// Get sms campaigns list.
    /// </summary>
    /// <returns>The sms campaigns list.</returns>
    /// <param name="dateFrom">Date from.</param>
    /// <param name="dateTo">Date to.</param>
    SendPulseResponse GetSmsCampaignsList(string dateFrom, string dateTo);
    /// <summary>
    /// Get sms campaign info.
    /// </summary>
    /// <returns>The sms campaign info.</returns>
    /// <param name="id">Identifier.</param>
    SendPulseResponse GetSmsCampaignInfo(int id);
    /// <summary>
    /// Cancel sms campaign.
    /// </summary>
    /// <returns>The sms campaign.</returns>
    /// <param name="id">Identifier.</param>
    SendPulseResponse CancelSmsCampaign(int id);
    /// <summary>
    /// Get sms campaign cost.
    /// </summary>
    /// <returns>The sms campaign cost.</returns>
    /// <param name="body">Body.</param>
    /// <param name="sender">Sender.</param>
    /// <param name="addressBookId">Address book identifier.</param>
    /// <param name="phones">Phones.</param>
    SendPulseResponse GetSmsCampaignCost(string body, string sender, int addressBookId = 0, string phones = "");
    /// <summary>
    /// Delete sms campaign.
    /// </summary>
    /// <returns>The sms campaign.</returns>
    /// <param name="id">Identifier.</param>
    SendPulseResponse DeleteSmsCampaign(int id);
    /// <summary>
    /// Add phones to addreess book.
    /// </summary>
    /// <returns>The phones to addreess book.</returns>
    /// <param name="addressBookId">Address book identifier.</param>
    /// <param name="phones">Phones.</param>
    SendPulseResponse AddPhonesToAddreessBook(int addressBookId, string phones);
    /// <summary>
    /// Send viber campaign.
    /// </summary>
    /// <param name="viberCampaign">Viber campaign to create</param>
    /// <returns>The viber campaign.</returns>
    SendPulseResponse SendViberCampaign(ViberCampaign viberCampaign);
    /// <summary>
    /// Get viber senders list.
    /// </summary>
    /// <returns>The viber senders.</returns>
    SendPulseResponse GetViberSenders();
    /// <summary>
    /// Get viber tasks list.
    /// </summary>
    /// <returns>The viber tasks list.</returns>
    /// <param name="limit">Limit.</param>
    /// <param name="offset">Offset.</param>
    SendPulseResponse GetViberTasksList(int limit = 100, int offset = 0);
    /// <summary>
    /// Get viber campaign statistic.
    /// </summary>
    /// <returns>The viber campaign stat.</returns>
    /// <param name="id">Identifier.</param>
    SendPulseResponse GetViberCampaignStat(int id);
    /// <summary>
    /// Get the viber sender info.
    /// </summary>
    /// <returns>The viber sender.</returns>
    /// <param name="id">Identifier.</param>
    SendPulseResponse GetViberSender(int id);
    /// <summary>
    /// Get viber task recipients.
    /// </summary>
    /// <returns>The viber task recipients.</returns>
    /// <param name="id">Identifier.</param>
    SendPulseResponse GetViberTaskRecipients(int id);
}